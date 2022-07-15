using Eto;
using Eto.Forms;
using GLib;
using RayTracer.Display.Dev.Appearance;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using static Serilog.Log;
using Application = Eto.Forms.Application;
using Log = Serilog.Log;
using Logger = RayTracer.Core.Logger;
using Thread = System.Threading.Thread;
using static RayTracer.Display.Dev.Program.ExitCode;
using UnhandledExceptionEventArgs = System.UnhandledExceptionEventArgs;

namespace RayTracer.Display.Dev;

/// <summary>Bootstrap class that contains the <c>Main()</c> function that inits everything else.</summary>
internal static class Program
{

	[SuppressMessage("ReSharper.DPA", "DPA0001: Memory allocation issues")] //Mainly due to Eto.Forms doing it's own thing
	private static int Main(string[] args) => (int)MainInternal(args);

	[SuppressMessage("ReSharper.DPA", "DPA0001: Memory allocation issues")] //Mainly due to Eto.Forms doing it's own thing
	private static ExitCode MainInternal(string[] args)
	{
		//Console and init
		Console.ForegroundColor = ConsoleColor.Blue;
		Console.Title           = "RayTracer [Console]";
		Console.WriteLine("RayTracer.Display.Dev: Starting program");
		Console.WriteLine("RayTracer.Display.Dev: Initialising logger");
		Logger.Init();
		Information("Starting RayTracer.Display.Dev app");
		Information("Commandline args: {Args}", args);

		try
		{
			Debug("Setting up AppDomain exception catchers");
			AppDomain.CurrentDomain.UnhandledException   += CurrentDomainOnUnhandledException;
			/*
			 * First-chance exceptions are often fine, since they'll be caught most of the time,
			 * but sometimes async exceptions can get hidden and suppressed, despite being uncaught AND are missed by AppDomain.UnhandledException
			 * So log them just in case, but they're unimportant hence the verbose
			 */
			void FirstChanceException(object? sender, FirstChanceExceptionEventArgs eventArgs)
			{
				Verbose(eventArgs.Exception, "First-chance exception from {Sender}:", sender);
			}
			AppDomain.CurrentDomain.FirstChanceException += FirstChanceException;
			Debug("Set up AppDomain exception catchers");
		}
		catch (Exception e)
		{
			Fatal(e, "Could not set up app domain exception handlers");
			return InitializationFailure;
		}

		try
		{
			throw new Exception();
		}
		catch
		{

		}

		Platform platform;
		try
		{
			Debug("Getting platform (Detect Mode)");
			platform = Platform.Detect!;
			Debug("Got Platform: {Platform}", platform.ID);
		}
		catch (Exception e)
		{
			Fatal(e, "Could not initialise Eto.Forms platform");
			return InitializationFailure;
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
			Debug("Created application object: {Application}", application);
		}
		catch (Exception e)
		{
			Fatal(e, "Could not initialise Eto.Forms application");
			return InitializationFailure;
		}

		try
		{
			Debug("Hooking up unhandled exception event");
			EventHandler<Eto.UnhandledExceptionEventArgs> unhandledExceptionHandler = EtoUnhandledException;
			application.UnhandledException += unhandledExceptionHandler;
			Debug("Unhandled exception event handler added: {@Handler}", unhandledExceptionHandler);
		}
		catch (Exception e)
		{
			Fatal(e, "Failed to set up unhandled exception handler");
			return InitializationFailure;
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
			Debug("Created new MainForm: {MainForm}", mainForm);
		}
		catch (Exception e)
		{
			Fatal(e, "Could not initialise MainForm");
			return InitializationFailure;
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
			return InitializationFailure;
		}

		try
		{
			Information("Running App");
			application.Run(mainForm);
			Information("App ran to completion");
			return GracefulExit;
		}
		catch (Exception e)
		{
			Fatal(e, "App threw exception");
			return AppFailure;
		}
		finally
		{
			//Not sure what exactly needs to be disposed, but I'll do it all just to be sure since I did get some warns about not disposed code from GLib before
			try
			{
				Debug("Disposing application objects and quitting");
				Verbose($"Disposing {nameof(mainForm)}");
				mainForm.Dispose();
				Verbose($"Disposing {nameof(application)}");
				application.Dispose();
				Debug("Disposed app and main form");
			}
			catch (Exception e)
			{
				Error(e, "Could not dispose application");
			}

			Information("Shutting down logger and exiting");
			CloseAndFlush();
			Console.WriteLine("Logger closed");
		}
	}

	private static void OnUnhandledException(Exception exception, object? sender, bool isTerminating)
	{
		Error(exception, "Caught unhandled exception ({Terminating}) from {Sender}:", isTerminating ? "terminating" : "non-terminating", sender);
	}

	private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		OnUnhandledException((Exception)e.ExceptionObject, sender, e.IsTerminating);
	}

	private static void EtoUnhandledException(object? sender, Eto.UnhandledExceptionEventArgs e)
	{
		OnUnhandledException((Exception)e.ExceptionObject, sender, e.IsTerminating);
	}

	public enum ExitCode
	{
		/// <summary>
		/// The exit was purposeful (e.g. the user clicked the quit button), and not due to exceptional circumstances
		/// </summary>
		GracefulExit = 0,
		/// <inheritdoc cref="GracefulExit"/>
		None = GracefulExit,
		/// <summary>
		/// The app failed during the initialization stage
		/// </summary>
		InitializationFailure,
		/// <summary>
		/// App failed while running
		/// </summary>
		AppFailure
	}
}