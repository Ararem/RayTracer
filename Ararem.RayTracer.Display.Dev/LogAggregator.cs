using static Serilog.Log;

namespace Ararem.RayTracer.Display.Dev;

/// <summary>Static helper class that combines logs from multiple other sources into our <see cref="Serilog"/> <see cref="Serilog.Log.Logger"/></summary>
internal static class LogAggregator
{
	public static void Init()
	{
		Debug("Setting up log aggregation");

		GLib();
	}

	private static void GLib()
	{
		#if PLATFORM_GTK
		/*
		 * You may need to add the following environment variables for these to be logged
		 *G_MESSAGES_DEBUG=all
		 * G_ENABLE_DIAGNOSTIC=1
		 */
		Debug("Redirecting GLib logs");
		Verbose(
				"SetDefaultHandler: {@Value}", global::GLib.Log.SetDefaultHandler(
						delegate(string domain, global::GLib.LogLevelFlags level, string message)
						{
							Debug("GLib - {Domain} @ {Level}: {Message}", domain, level, message);
						}
				)
		);
		Verbose(
				"SetPrintHandler: {@Value}", global::GLib.Log.SetPrintHandler(
						delegate(string message)
						{
							Debug("GLib: {Message}", message);
						}
				)
		);
		Verbose(
				"SetPrintErrorHandler: {@Value}", global::GLib.Log.SetPrintErrorHandler(
						delegate(string message)
						{
							Debug("GLib: {Message}", message);
						}
				)
		);
		Debug("GLib redirected");
		#else
		Verbose("Platform is not GLib so skipping redirect");
		#endif
	}
}