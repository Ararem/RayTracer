using Eto;
using Eto.Forms;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using System;

namespace RayTracer.Display.EtoForms;
using static Log;

internal static class Program
{
	[STAThread]
	private static int Main(string[] args)
	{
		Core.Logger.Init();
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
		Application application = new Application(platform);
		MainForm    form;
		try
		{
			// ReSharper disable AssignNullToNotNullAttribute
			Logger = new LoggerConfiguration()
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
	}
}