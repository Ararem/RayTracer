using GLib;
using Serilog.Events;
using System;
using System.Reflection;
using DateTime = System.DateTime;

namespace Ararem.RayTracer.Display.Dev;

/// <summary>Static helper class that combines logs from multiple other sources into our <see cref="Serilog"/> <see cref="Serilog.Log.Logger"/></summary>
//TODO: Aardvark and GLib
internal static class LogAggregator
{
	public static void Init()
	{
		Debug("Setting up log aggregation");

		GLib();
	}

	private static void GLib()
	{
		/*
		 * You may need to add the following environment variables for these to be logged
		 *G_MESSAGES_DEBUG=all
		 * G_ENABLE_DIAGNOSTIC=1
		 */
		Debug("Redirecting GLib logs");
		Log.SetDefaultHandler(
				delegate(string domain, LogLevelFlags level, string message)
				{
					Debug("GLib - {Domain} @ {Level}: {Message}", domain, level, message);
				}
		);
		// Log.SetLogHandler(
		// 		"", LogLevelFlags.All, delegate(string domain, LogLevelFlags level, string message)
		// 		{
		// 			Debug("GLib.Log - {Domain} @ {Level}: {Message}", domain, level, message);
		// 		}
		// );
		// new Log().WriteLog("asdasdasdasd", LogLevelFlags.Debug, "Testing");

		Log.SetPrintHandler(
				delegate(string message)
				{
					Debug("GLib: {Message}", message);
				}
		);
		Log.SetPrintErrorHandler(
				delegate (string message)
				{
					Debug("GLib: {Message}", message);
				}
		);
		Debug("GLib redirected");
	}
}