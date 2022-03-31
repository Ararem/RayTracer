using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace RayTracer.Core;

internal static class Logger
{
	private const string template = "[{Timestamp:HH:mm:ss} {ThreadName}@{Level:u3}] {Message:lj}{NewLine}{Exception}";
	internal static void Init()
	{
		Log.Logger = new LoggerConfiguration()
					.WriteTo!.Console(outputTemplate:template,applyThemeToRedirectedOutput: true, theme: AnsiConsoleTheme.Code)
					.Enrich.WithThreadId()!
					.Enrich.WithThreadName()!
					.CreateLogger();
		Log.Information("Logger Initialized");
	}
}