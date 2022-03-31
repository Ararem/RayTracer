using Eto;
using Eto.Forms;
using RayTracer.Display.EtoForms;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using System;
using static Serilog.Log;
using Logger = RayTracer.Core.Logger;

Logger.Init();
Information("Commandline args: {Args}", args);

Platform platform;
try
{
	Verbose("Getting platform");
	platform = Platform.Detect!;
	Verbose("Got Platform");
}
catch (Exception e)
{
	Fatal(e, "Could not initialise Eto.Forms platform");
	return -1;
}

Verbose("Platform is {Platform}", platform);
Application application = new(platform);
MainForm    form;
try
{
	// ReSharper disable AssignNullToNotNullAttribute
	Log.Logger = new LoggerConfiguration()
				.WriteTo.Console(applyThemeToRedirectedOutput: true, theme: AnsiConsoleTheme.Code)
				.CreateLogger();
	Information("Logger Initialized");
	// ReSharper restore AssignNullToNotNullAttribute
	Verbose("Creating MainForm");
	form = new MainForm();
	Verbose("Created MainForm");
}
catch (Exception e)
{
	Fatal(e, "Could not initialise MainForm");
	return -1;
}
// Verbose("MainForm is {MainForm}", form);

Information("Running App with ");
application.Run(form);
return 0;