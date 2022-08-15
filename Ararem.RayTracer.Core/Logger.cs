using Destructurama;
using LibArarem.Core.Logging.Destructurers;
using LibArarem.Core.Logging.Enrichers;
using LibArarem.Core.ObjectPools;
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
using static LibArarem.Core.Logging.Enrichers.InstanceContextEnricher;
using static LibArarem.Core.Logging.Enrichers.ThreadInfoEnricher;
using static Serilog.Log;

namespace Ararem.RayTracer.Core;

internal static class Logger
{
	private static readonly Process CurrentProcess = Process.GetCurrentProcess();

	/// <summary>Internal flag to enable full logging (e.g. stacktraces, etc)</summary>
	private static bool ExtendedLog { get; } = Environment.GetEnvironmentVariable(nameof(ExtendedLog)) is not null and not "false" and not "0" and not "off";

	internal static void Init(Func<LoggerConfiguration, LoggerConfiguration>? earlyAdjustConfig = null, Func<LoggerConfiguration, LoggerConfiguration>? lateAdjustConfig = null)
	{
		string template = StringBuilderPool.BorrowInline(
				static sb =>
				{
					const string spacer = " | "; //String to go between details

					string?[] details =
					{
							ExtendedLog ? "{Timestamp:O}" : "{Timestamp:HH:mm:ss.fff}", //[DateTime] Date and time of the log event (6/9/2020 4:20 pm for example)
							"+{TimestampFromStart:G}", //[TimeSpan] Time offset since the process started
							$"{{{LogEventNumberEnricher.EventNumberProp},9:'Evt #'####}}", //[ulong] Number of this log event
							ExtendedLog ? $"{{{ThreadNameProp},-30}} ({{{ThreadTypeProp},11}})" : null, //[strings] Name and type of the thread that the log call was on
							$"{{{ThreadIdProp},10:'Thread #'##}}", //[int] Managed ID of the thread that the log call was on
							$"{{{CallingTypeNameProp},22}}.{{{CallingMethodNameProp},-30}}@ {{{CallingMethodLineProp},3:n0}}:{{{CallingMethodColumnProp}:n0}}", //What piece of code made the call
							"{Level:t3}", //[LogEventLevel] Severity of the event,
							$"{{{InstanceContextProp}}}"
					};

					sb.Append('[');                                                           //Start details
					sb.AppendJoin(spacer, details.Where(s => !string.IsNullOrWhiteSpace(s))); //Add details, skipping any that are null
					sb.Append(']');                                                           //End details
					sb.Append('\t');                                                          //Add a quick spacer
					if (!ExtendedLog) sb.Append($"{{{LevelIndentProp}}}");                   //Add an indent (only really works when not extra verbose cause stacktraces split it up
					sb.Append("{Message:l}");                                                 //Log message body
					sb.Append("{NewLine}");
					sb.Append($"{{Exception}}{{{ExceptionDataProp}}}"); //Exception things
					if (ExtendedLog)
					{
						sb.Append($"{{{StackTraceProp}}}{{NewLine}}");
						sb.Append("{Properties}{NewLine}");
						sb.Append("{NewLine}");
					}
				}
		);
		PerfMode perfMode = ExtendedLog ? PerfMode.FullTraceSlow : PerfMode.SingleFrameFast;
		Thread.CurrentThread.Name ??= "Main Thread";
		SelfLog.Enable(str => Console.Error.WriteLine($"[SelfLog] {str}"));
		LoggerConfiguration config = new();
		config = earlyAdjustConfig?.Invoke(config) ?? config; //If no config func is provided, `Invoke(...)` isn't called and it returns null, which is handled by the `?? config`
		config = config.MinimumLevel.Is(LogEventLevel.Verbose)
					   .WriteTo.Console(outputTemplate: template, applyThemeToRedirectedOutput: true, theme: AnsiConsoleTheme.Code)
					   .Enrich.FromLogContext()
					   .Enrich.With<ExceptionDataEnricher>()
					   .Enrich.WithDemystifiedStackTraces()
					   .Enrich.With<ThreadInfoEnricher>()
					   .Enrich.With<EventLevelIndentEnricher>()
					   .Enrich.With<LogEventNumberEnricher>()
					   .Enrich.With(new CallerContextEnricher(perfMode))
					   .Enrich.With(new DynamicEnricher("TimestampFromStart", static () => DateTime.Now - CurrentProcess.StartTime))
					   .Enrich.FromLogContext()
					   .Destructure.With<DelegateDestructurer>()
					   .Destructure.With<IncludePublicFieldsDestructurer>()
					   .Destructure.AsScalar<Vector3>()
					   .Destructure.UsingAttributes()
					   .Destructure.AsScalar<Vector2>();

		config = lateAdjustConfig?.Invoke(config) ?? config; //If no config func is provided, `Invoke(...)` isn't called and it returns null, which is handled by the `?? config`

		Log.Logger = config.CreateLogger();

		Information("Logger Initialised");
	}
}