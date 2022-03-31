using Eto;
using Eto.Forms;
using RayTracer.Display.EtoForms;
using System;
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
	platform = Platform.Detect!;
}
catch (Exception e)
{
	Fatal(e, "Could not initialise Eto.Forms platform");
	return -1;
}

Verbose("Platform is {Platform}", platform);

Application application;
try
{
	application = new Application(platform);
}
catch (Exception e)
{
	Fatal(e, "Could not initialise Eto.Forms application");
	return -1;
}

Verbose("Application is {Application}", application);

MainForm form;
try
{
	form = new MainForm();
}
catch (Exception e)
{
	Fatal(e, "Could not initialise MainForm");
	return -1;
}

Verbose("MainForm is {MainForm}", form);

try
{
	Information("Running App");
	application.Run(form);
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