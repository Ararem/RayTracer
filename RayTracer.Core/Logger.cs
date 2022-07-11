// #define DEBUG_LOG

using LibEternal.Core.Logging.Destructurers;
using LibEternal.Core.Logging.Enrichers;
using Serilog;
using Serilog.Debugging;
using Serilog.Enrichers;
using Serilog.Sinks.SystemConsole.Themes;
using System.Diagnostics;
using System.Numerics;
using static LibEternal.Core.Logging.Enrichers.ExceptionDataEnricher;
using static LibEternal.Core.Logging.Enrichers.CallerContextEnricher;
using static LibEternal.Core.Logging.Enrichers.EventLevelIndentEnricher;
using static LibEternal.Core.Logging.Enrichers.ThreadInfoEnricher;

namespace RayTracer.Core;

internal static class Logger
{
	private static readonly Process CurrentProcess = Process.GetCurrentProcess();

	internal static void Init()
	{
		#if DEBUG_LOG
		const PerfMode perfMode = PerfMode.FullTraceSlow;
		const string template = $"[{{Timestamp:HH:mm:ss}} | {{{LogEventNumberEnricher.EventNumberProp},5:'#'####}} | {{Level:t3}} | {{{ThreadNameProp},-30}} {{{ThreadIdProp},3:'#'##}} ({{{ThreadTypeProp},11}}) | {{{CallingTypeNameProp},10}}::{{{CallingMethodNameProp},-10}}]:\t{{{LevelIndentProp}}}{{Message:l}}{{NewLine}}{{Exception}}{{NewLine}}{{{StackTraceProp}}}{{NewLine}}{{NewLine}}";;
		#else
		const PerfMode perfMode = PerfMode.SingleFrameFast;
		const string   template = $"[{{Timestamp:HH:mm:ss}} | +{{AppTimestamp:G}} | {{Level:t3}} | " /*+ $"{{{ThreadNameProp},-30}} " /**/+ $"{{{ThreadIdProp},3:'#'##}} | {{{CallingTypeNameProp},30}}::{{{CallingMethodNameProp},-20}}] {{{LevelIndentProp}}}{{Message:l}}{{NewLine}}{{Exception}}{{{ExceptionDataProp}}}";
		#endif

		Thread.CurrentThread.Name ??= "Main Thread";
		// ReSharper disable PossibleNullReferenceException
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
		// ReSharper restore PossibleNullReferenceException

		Log.Information("Logger Initialized");
	}
}