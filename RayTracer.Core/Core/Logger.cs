using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace RayTracer.Core;

internal static class Logger
{
	private const string Template = "[{Timestamp:HH:mm:ss} {ThreadName}@{Level:u3}] {Message:lj}{NewLine}{Exception}";

	internal static void Init()
	{
		Thread.CurrentThread.Name ??= "Main Thread";
		// ReSharper disable PossibleNullReferenceException
		Log.Logger = new LoggerConfiguration()
					.MinimumLevel.Verbose()
					.WriteTo.Console(outputTemplate:Template,applyThemeToRedirectedOutput: true, theme: AnsiConsoleTheme.Code)
					.Enrich.WithThreadId()
					.Enrich.WithThreadName()!
					.CreateLogger();
		// ReSharper restore PossibleNullReferenceException

		Log.Information("Logger Initialized");
	}
}