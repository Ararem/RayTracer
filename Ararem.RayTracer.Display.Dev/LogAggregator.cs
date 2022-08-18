using Microsoft.VisualBasic.Logging;
using System;
using System.Linq.Expressions;
using System.Runtime.Versioning;
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

	[SupportedOSPlatformGuard("linux")]
	private static void GLib()
	{
		const string gLibLogClassName = "GLib.Log", gLibLogClassAssembly = "GLibSharp";
		Type         gLibLogClass;
		try
		{
			gLibLogClass = Type.GetType($"{gLibLogClassName}, {gLibLogClassAssembly}", true)!;
		}
		catch (Exception e)
		{
			Error(e, "Could not load GLib Log class ({ClassName}) in assembly {AssemblyName}", gLibLogClassName, gLibLogClassAssembly);
			return;
		}

		void RedirectFunc<T>(string methodName, T @delegate) where T : Delegate
		{
			Verbose("Redirection method {MethodName} with delegate {@Delegate}", methodName, @delegate);
			try
			{

			}
			catch (Exception e)
			{
				Warning(e, "Could not redirect method");
			}
			Verbose("Redirected");
		}

		/*
		 * You may need to add the following environment variables for these to be logged
		 *G_MESSAGES_DEBUG=all
		 * G_ENABLE_DIAGNOSTIC=1
		 */

		Debug("Redirecting GLib logs");
		Verbose(
				"SetDefaultHandler",
						delegate(string domain, LogLevelFlags level, string message)
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
	}
#endif
}