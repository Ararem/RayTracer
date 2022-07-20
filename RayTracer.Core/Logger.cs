// #define DEBUG_LOG

using LibArarem.Core.Logging.Destructurers;
using LibArarem.Core.Logging.Enrichers;
using Serilog;
using Serilog.Debugging;
using Serilog.Enrichers;
using Serilog.Sinks.SystemConsole.Themes;
using System.Diagnostics;
using System.Numerics;
using static LibArarem.Core.Logging.Enrichers.ExceptionDataEnricher;
using static LibArarem.Core.Logging.Enrichers.CallerContextEnricher;
using static LibArarem.Core.Logging.Enrichers.EventLevelIndentEnricher;
using static LibArarem.Core.Logging.Enrichers.ThreadInfoEnricher;

namespace RayTracer.Core;

internal static class Logger
{
	private static readonly Process CurrentProcess = Process.GetCurrentProcess();

	internal static void Init()
	{
		#if DEBUG_LOG
		const PerfMode perfMode = PerfMode.FullTraceSlow;
		const string template = $"[{{Timestamp:HH:mm:ss}} | {{{LogEventNumberEnricher.EventNumberProp},5:'#'####}} | {{Level:t3}} | {{{ThreadNameProp},-30}} {{{ThreadIdProp},3:'#'##}} ({{{ThreadTypeProp},11}}) | {{{CallingTypeNameProp},20}}.{{{CallingMethodNameProp},-30}}@{{{CallingMethodLineProp},3:n0}}:{{{CallingMethodColumnProp},-2:n0}}]:\t{{{LevelIndentProp}}}{{Message:l}}{{NewLine}}{{Exception}}{{NewLine}}{{{StackTraceProp}}}{{NewLine}}{{NewLine}}";;
		#else
		const PerfMode perfMode = PerfMode.SingleFrameFast;
		const string   template = "[{Timestamp:HH:mm:ss} | +{AppTimestamp:G} | {Level:t3} | " /*+ $"{{{ThreadNameProp},-30}} " /**/ + $"{{{ThreadIdProp},10:'Thread #'##}} | {{{CallingTypeNameProp},20}}.{{{CallingMethodNameProp},-30}}@{{{CallingMethodLineProp},3:n0}}] {{{LevelIndentProp}}}{{Message:l}}{{NewLine}}{{Exception}}{{{ExceptionDataProp}}}";
		#endif

		Thread.CurrentThread.Name ??= "Main Thread";
		SelfLog.Enable(Console.Error);
		Log.Logger = new LoggerConfiguration()
					.MinimumLevel.Verbose()
					.WriteTo.Console(outputTemplate: template, applyThemeToRedirectedOutput: true, theme: AnsiConsoleTheme.Code)
					.Enrich.WithThreadId()
					.Enrich.WithThreadName()
					.Enrich.FromLogContext()
					.Enrich.With<ExceptionDataEnricher>()
					.Enrich.With<DemystifiedExceptionsEnricher>()
					.Enrich.With<ThreadInfoEnricher>()
					.Enrich.With<EventLevelIndentEnricher>()
					.Enrich.With<LogEventNumberEnricher>()
					.Enrich.With(new CallerContextEnricher(perfMode))
					.Enrich.With(new DynamicEnricher("AppTimestamp", static () => DateTime.Now - CurrentProcess.StartTime))
					.Destructure.With<DelegateDestructurer>()
					.Destructure.With<IncludePublicFieldsDestructurer>()
					.Destructure.AsScalar<Vector3>()
					.Destructure.AsScalar<Vector2>()
					.CreateLogger();


		#if DEBUG
		if (debugOnlyUpdateLogConfigTimer == null)
		{
			Log.Information("Logger Initialized");
			debugOnlyUpdateLogConfigTimer = new Timer(DebugOnly_UpdateLogConfig, null, 0, 1000);
		}
		#else
		Log.Information("Logger Initialized");
		#endif
	}
	#if DEBUG

	private static Timer? debugOnlyUpdateLogConfigTimer = null;
	private static void DebugOnly_UpdateLogConfig(object? state)
	{
		Init();
	}
	#endif
}