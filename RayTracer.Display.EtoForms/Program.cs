using Eto;
using RayTracer.Display.EtoForms.Appearance;
using System;
using System.Diagnostics.CodeAnalysis;
using static Serilog.Log;
using Application = Eto.Forms.Application;
using Logger = RayTracer.Core.Logger;
using UnhandledExceptionEventArgs = Eto.UnhandledExceptionEventArgs;

namespace RayTracer.Display.EtoForms;

internal static class Program
{
	[SuppressMessage("ReSharper.DPA", "DPA0001: Memory allocation issues")] //Mainly due to Eto.Forms doing it's own thing
	private static int Main(string[] args)
	{
		Logger.Init();
		Information("Commandline args: {Args}", args);

		Platform platform;
		try
		{
			Verbose("Getting platform (Detect Mode)");
			platform = Platform.Detect!;
			Verbose("Got platform");
		}
		catch (Exception e)
		{
			Fatal(e, "Could not initialise Eto.Forms platform");
			return -1;
		}

		Debug("Platform is {Platform}", platform);

		Application application;
		try
		{
			Verbose("Creating new application object");
			application = new Application(platform)
			{
					Name = "RayTracer Application",
					ID   = "Main Application"
			};
			Verbose("Created new application object");
		}
		catch (Exception e)
		{
			Fatal(e, "Could not initialise Eto.Forms application");
			return -1;
		}

		Debug("Application is {Application}", application);

		Verbose("Hooking up unhandled exception event");
		application.UnhandledException      += EtoUnhandledException;
		Verbose("Created new application object");

		Debug("Application is {Application}", application);

		try
		{
			Styles.RegisterStyles();
		}
		catch (Exception e)
		{
			Error(e, "Could not register styles");
		}

		MainForm form;
		try
		{
			Verbose("Creating new MainForm");
			form = new MainForm();
			Verbose("Created new MainForm");
		}
		catch (Exception e)
		{
			Fatal(e, "Could not initialise MainForm");
			return -1;
		}

		Debug("MainForm is {MainForm}", form);

		Debug("Starting task watcher");
		TaskWatcher.Init();

		try
		{
			Information("Running App");
			application.Run(form);
			Information("App ran to completion");
			return 0;
		}
		catch (Exception e)
		{
			Fatal(e, "App threw exception");
			return -1;
		}
	}

	private static void EtoUnhandledException(object? obj, UnhandledExceptionEventArgs args) => Error((Exception)args.ExceptionObject, "Caught ETO Unhandled {IsTerminating} Exception from {Target}", args.IsTerminating ? "Terminating" : "Non-Terminating", obj);
}