using Eto;
using Eto.Forms;
using RayTracer.Display.Dev.Appearance;
using System;
using System.Diagnostics.CodeAnalysis;
using static Serilog.Log;
using Logger = RayTracer.Core.Logger;
using UnhandledExceptionEventArgs = Eto.UnhandledExceptionEventArgs;

namespace RayTracer.Display.Dev;

/// <summary>Bootstrap class that contains the <c>Main()</c> function that inits everything else.</summary>
internal static class Program
{
	[SuppressMessage("ReSharper.DPA", "DPA0001: Memory allocation issues")] //Mainly due to Eto.Forms doing it's own thing
	private static int Main(string[] args)
	{
		//Console and init
		Console.ForegroundColor = ConsoleColor.Blue;
		Console.Title           = "RayTracer [Console]";
		Console.WriteLine("RayTracer.Display.Dev: Starting program");
		Console.WriteLine("RayTracer.Display.Dev: Initialising logger");
		Logger.Init();
		Information("Starting RayTracer.Display.Dev app");
		Information("Commandline args: {Args}", args);

		Platform platform;
		try
		{
			Debug("Getting platform (Detect Mode)");
			platform = Platform.Detect!;
			Debug("Eto.Forms Platform: {@Platform}", platform);
		}
		catch (Exception e)
		{
			Fatal(e, "Could not initialise Eto.Forms platform");
			return -1;
		}

		Application application;
		try
		{
			Debug("Creating new application object");
			application = new Application(platform)
			{
					Name              = "RayTracer.Display",
					ID                = "Main Application",
					UIThreadCheckMode = UIThreadCheckMode.Error,
					BadgeLabel        = "RayTracer [Badge]"
			};
			Debug("Created application object: {@Application}", application);
		}
		catch (Exception e)
		{
			Fatal(e, "Could not initialise Eto.Forms application");
			return -1;
		}

		try
		{
			EventHandler<UnhandledExceptionEventArgs> unhandledExceptionHandler = EtoUnhandledException;
			Debug("Hooking up unhandled exception event");
			application.UnhandledException += unhandledExceptionHandler;
			Debug("Unhandled exception event handler added: {@Handler}", unhandledExceptionHandler);
		}
		catch (Exception e)
		{
			Fatal(e, "Failed to set up unhandled exception handler");
			return -1;
		}

		try
		{
			Debug("Registering styles");
			Styles.RegisterStyles();
			Debug("Styles registered");
		}
		catch (Exception e)
		{
			Error(e, "Could not register styles");
		}

		MainForm mainForm;
		try
		{
			Debug("Creating new MainForm");
			mainForm = new MainForm();
			Debug("Created new MainForm: {@MainForm}", mainForm);
		}
		catch (Exception e)
		{
			Fatal(e, "Could not initialise MainForm");
			return -1;
		}

		try
		{
			Debug("Starting task watcher");
			TaskWatcher.Init();
			Debug("Task watcher started");
		}
		catch (Exception e)
		{
			Fatal(e, "Failed to initialize task watcher");
			return -1;
		}

		try
		{
			Information("Running App");
			application.Run(mainForm);
			Information("App ran to completion");
			return 0;
		}
		catch (Exception e)
		{
			Fatal(e, "App threw exception");
			return -1;
		}
		finally
		{
			//Not sure what exactly needs to be disposed, but I'll do it all just to be sure since I did get some warns about not disposed code from GLib before
			Debug("Disposing application objects and quitting");
			Verbose($"Disposing {nameof(mainForm)}");
			mainForm.Dispose();
			Verbose($"Disposing {nameof(application)}");
			application.Dispose();
			Debug("Disposed app and main form");

			Information("Shutting down logger and exiting");
			CloseAndFlush();
			Console.WriteLine("Logger closed");
		}
	}

	private static void EtoUnhandledException(object? obj, UnhandledExceptionEventArgs args) => Error((Exception)args.ExceptionObject, "Caught ETO Unhandled {IsTerminating} Exception from {Target}", args.IsTerminating ? "Terminating" : "Non-Terminating", obj);
}