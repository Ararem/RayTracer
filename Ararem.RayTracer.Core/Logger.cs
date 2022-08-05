// #define DEBUG_LOG

using Destructurama;
using LibArarem.Core.Logging.Destructurers;
using LibArarem.Core.Logging.Enrichers;
using Serilog;
using Serilog.Debugging;
using Serilog.Enrichers;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using System.Diagnostics;
using System.Numerics;
using static LibArarem.Core.Logging.Enrichers.ExceptionDataEnricher;
using static LibArarem.Core.Logging.Enrichers.CallerContextEnricher;
using static LibArarem.Core.Logging.Enrichers.EventLevelIndentEnricher;
using static LibArarem.Core.Logging.Enrichers.ThreadInfoEnricher;
using static Serilog.Log;

namespace Ararem.RayTracer.Core;

internal static class Logger
{
	private static readonly Process CurrentProcess = Process.GetCurrentProcess();

	internal static void Init(Func<LoggerConfiguration, LoggerConfiguration>? adjustConfig = null)
	{
		#if DEBUG_LOG
		const PerfMode perfMode = PerfMode.FullTraceSlow;
		const string template = $"[{{Timestamp:HH:mm:ss.ffffff}} | {{{LogEventNumberEnricher.EventNumberProp},5:'#'####}} | {{Level:t3}} | {{{ThreadNameProp},-30}} {{{ThreadIdProp},3:'#'##}} ({{{ThreadTypeProp},11}}) | {{{CallingTypeNameProp},20}}.{{{CallingMethodNameProp},-30}}@{{{CallingMethodLineProp},3:n0}}:{{{CallingMethodColumnProp},-2:n0}}]:\t{{{LevelIndentProp}}}{{Message:l}}{{NewLine}}{{Exception}}{{NewLine}}{{{StackTraceProp}}}{{NewLine}}{{NewLine}}";;
		#else
		const PerfMode perfMode = PerfMode.SingleFrameFast;
		const string   template = "[{Timestamp:HH:mm:ss.ffffff} | +{AppTimestamp:G} | {Level:t3} | " /*+ $"{{{ThreadNameProp},-30}} " /**/ + $"{{{ThreadIdProp},10:'Thread #'##}} | {{{CallingTypeNameProp},20}}.{{{CallingMethodNameProp},-30}}@{{{CallingMethodLineProp},3:n0}}] {{{LevelIndentProp}}}{{Message:l}}{{NewLine}}{{Exception}}{{{ExceptionDataProp}}}";
		#endif
		Thread.CurrentThread.Name ??= "Main Thread";
		SelfLog.Enable(str => Console.Error.WriteLine($"[SelfLog] {str}"));
		LoggerConfiguration config = new LoggerConfiguration().MinimumLevel.Is(LogEventLevel.Verbose).WriteTo.Console(outputTemplate: template, applyThemeToRedirectedOutput: true, theme: AnsiConsoleTheme.Code).Enrich.WithThreadId().Enrich.WithThreadName().Enrich.FromLogContext().Enrich.With<ExceptionDataEnricher>().Enrich.With<DemystifiedExceptionsEnricher>().Enrich.With<ThreadInfoEnricher>().Enrich.With<EventLevelIndentEnricher>().Enrich.With<LogEventNumberEnricher>().Enrich.With(new CallerContextEnricher(perfMode)).Enrich.With(new DynamicEnricher("AppTimestamp", static () => DateTime.Now - CurrentProcess.StartTime)).Destructure.With<DelegateDestructurer>().Destructure.With<IncludePublicFieldsDestructurer>().Destructure.AsScalar<Vector3>().Destructure.UsingAttributes().Destructure.AsScalar<Vector2>();

		config = adjustConfig?.Invoke(config) ?? config; //If no config func is provided, `Invoke(...)` isn't called and it returns null, which is handled by the `?? config`

		Log.Logger = config.CreateLogger();

		Information("Logger Initialised");
	}
}