using JetBrains.Annotations;
using LibArarem.Core.Logging;
using NetFabric.Hyperlinq;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Ararem.RayTracer.Display.Dev;

/// <summary>
/// Static helper class for 'watching' tasks (making sure they complete successfully without manually having to check them)
/// </summary>
public static class TaskWatcher
{
	private static readonly ConcurrentBag<(Task Task, bool ExitOnError)> WatchedTasks    = new();
	private static readonly ConcurrentBag<(Task Task, bool ExitOnError)> NotYetCompleted = new();

	[UsedImplicitly] private static Timer watcherTimer = null!;

	/// <summary>Allows you to 'watch' a task for errors while discarding it. Useful when you want async events but are only given a sync invoker.</summary>
	/// <param name="task"></param>
	/// <param name="exitOnError"></param>
	/// <remarks>To use, simply call the async method you want to watch, and then call <see cref="Watch"/> on the returned task object</remarks>
	public static void Watch(Task task, bool exitOnError)
	{
		Log.Verbose("Added watched task {Task} (Exit on error = {ExitOnError}", task, exitOnError);
		WatchedTasks.Add((task, exitOnError));
	}

	/// <summary>Initializes the task watcher. Make sure to only call this once</summary>
	internal static void Init()
	{
		const int period = 500;
		Log.Debug("Task watcher started with period of {Period}", period);
		watcherTimer = new Timer(CheckTasks, null, 0, period);
	}

	private static void CheckTasks(object? _)
	{
		try
		{
			using IDisposable __              = LogUtils.MarkContextAsExtremelyVerbose();
			int               totalTaskCount = 0, erroredTaskCount = 0, completeTaskCount = 0, incompleteTaskCount = 0;
			//Loop over all the tasks to check
			while (WatchedTasks.TryTake(out (Task Task, bool ExitOnError) tuple))
			{
				(Task task, bool exitOnError) = tuple;
				Log.Verbose("Checking task {@Task}", task);

				totalTaskCount++;
				if (task.IsFaulted)
				{
					erroredTaskCount++;
					Log.Error(task.Exception!, "Caught exception in watched task {Task}", task);
					if (exitOnError)
					{
						const int exitCode = (int)Program.ExitCode.AppFailure;
						Log.Debug("Exiting with Environment.Exit({ExitCode})", exitCode);
						//Don't call App.Quit here, since it may throw
						Environment.Exit(exitCode);
					}
				}
				else if (!task.IsCompleted)
				{
					incompleteTaskCount++;
					NotYetCompleted.Add(tuple);
					Log.Verbose("Task {@Task} not yet completed", task);
				}
				//If the task has completed, no error, we don't do anything
				//Since it's already been removed from the bag
				else
				{
					completeTaskCount++;
					Log.Verbose("Task {@Task} was completed", task);
				}
			}

			//Add back the ones that haven't finished
			while (NotYetCompleted.TryTake(out (Task Task, bool ExitOnError) result)) WatchedTasks.Add(result);

			if(totalTaskCount !=0)
			Log.Verbose("Processed watched tasks: {Errored} errored, {Incomplete} incomplete, {Complete} completed, {Total} total", erroredTaskCount, incompleteTaskCount, completeTaskCount, totalTaskCount);
		}
		catch (Exception e)
		{
			const int exitCode = (int)Program.ExitCode.AppFailure;
			Log.Fatal(e, "Caught fatal exception when processing watched tasks");
			Log.Fatal("Program terminating ({ExitCode})",exitCode);
			Environment.Exit(exitCode);
		}
	}
}