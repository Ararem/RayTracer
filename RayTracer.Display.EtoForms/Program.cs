using Eto;
using Eto.Forms;
using RayTracer.Display.EtoForms;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using static Serilog.Log;
using Logger = RayTracer.Core.Logger;

#if EXPLICIT_MAIN_FUNCTION
internal static class Program
{

	private static int Main(string[] args)
	{
#endif

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
	application = new Application(platform);
	Verbose("Created new application object");
}
catch (Exception e)
{
	Fatal(e, "Could not initialise Eto.Forms application");
	return -1;
}

Debug("Application is {Application}", application);

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
Task.Run(TaskWatcher.WatchTasksWorker);

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
#if EXPLICIT_MAIN_FUNCTION
	}
}
#endif