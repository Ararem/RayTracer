using Eto.Forms;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace RayTracer.Display.EtoForms;

public static class TaskWatcher
{
	private static readonly ConcurrentBag<(Task Task, bool ExitOnError)> WatchedTasks    = new();
	private static readonly ConcurrentBag<(Task Task, bool ExitOnError)> NotYetCompleted = new();

	/// <summary>
	///  Allows you to 'watch' a task for errors while discarding it. Useful when you want async events but are only given a sync invoker.
	/// </summary>
	/// <param name="task"></param>
	/// <param name="exitOnError"></param>
	/// <remarks>To use, simply call the async method you want to watch, and then call <see cref="Watch"/> on the returned task object</remarks>
	public static void Watch(Task task, bool exitOnError) => WatchedTasks.Add((task, exitOnError));

	internal static async Task WatchTasksWorker()
	{
		Log.Debug("Task watcher started");
		try
		{
			while (true)
			{
				while (WatchedTasks.TryTake(out (Task Task, bool ExitOnError) task))
				{
					//Log the error if there was one, else keep watching
					if (task.Task.IsFaulted)
					{
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
						NotYetCompleted.Add(task);
					}

					await Task.Delay(100);
				}

				foreach ((Task Task, bool ExitOnError) task in NotYetCompleted) WatchedTasks.Add(task);
			}
		}
		catch (Exception e)
		{
			Log.Fatal(e, "Caught fatal exception when watching tasks");
			Log.Fatal("Program terminating");
			if (Application.Instance.QuitIsSupported)
				Application.Instance.Quit();
			else Environment.Exit(-1);
		}
	}
}