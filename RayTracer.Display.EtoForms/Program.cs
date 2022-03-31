using Eto;
using Eto.Forms;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using System;

namespace RayTracer.Display.EtoForms;

internal class Program
{
	[STAThread]
	private static int Main(string[] args)
	{
		// ReSharper disable AssignNullToNotNullAttribute
		Log.Logger = new LoggerConfiguration()
					.WriteTo.Console(applyThemeToRedirectedOutput: true, theme: AnsiConsoleTheme.Code)
					.CreateLogger();
		Log.Information("Logger Initialized");
		// ReSharper restore AssignNullToNotNullAttribute

		new Application(Platform.Detect!).Run(new MainForm());
		return 0;
	}
}