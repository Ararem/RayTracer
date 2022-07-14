using Eto.Forms;
using JetBrains.Annotations;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace RayTracer.Display.Dev;

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
			int totalTaskCount = 0, erroredTaskCount = 0, completeTaskCount = 0, incompleteTaskCount = 0;
			//Loop over all the tasks to check
			while (WatchedTasks.TryTake(out (Task Task, bool ExitOnError) task))
			{
				totalTaskCount++;
				if (task.Task.IsFaulted)
				{
					erroredTaskCount++;
					Log.Error(task.Task.Exception, "Caught exception in watched task {@Task}", task);
					if (task.ExitOnError)
					{
						if (Application.Instance.QuitIsSupported)
							Application.Instance.Quit();
						else Environment.Exit(-1);
					}
				}
				else if (!task.Task.IsCompleted)
				{
					incompleteTaskCount++;
					NotYetCompleted.Add(task);
				}
				//If the task has completed, no error, we don't do anything
				//Since it's already been removed from the bag
				else
				{
					completeTaskCount++;
				}
			}

			//Add back the ones that haven't finished
			while (NotYetCompleted.TryTake(out (Task Task, bool ExitOnError) result)) WatchedTasks.Add(result);

			Log.Verbose("Processed watched tasks: {Errored} errored, {Incomplete} incomplete, {Complete} completed, {Total} total", erroredTaskCount, incompleteTaskCount, completeTaskCount, totalTaskCount);
		}
		catch (Exception e)
		{
			Log.Fatal(e, "Caught fatal exception when processing watched tasks");
			Log.Fatal("Program terminating");
			if (Application.Instance.QuitIsSupported)
				Application.Instance.Quit();
			else Environment.Exit(-1);
		}
	}
}