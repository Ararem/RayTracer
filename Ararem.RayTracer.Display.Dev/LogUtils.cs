using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static Serilog.Log;

namespace Ararem.RayTracer.Display.Dev;

public class LogUtils
{
	/// <summary>Logs a message that an event was called, allowing for callback tracing</summary>
	/// <example>
	///  <code>
	///  void ButtonClickedCallback(object? sender, EventArgs eventArgs){
	/// 		TraceEvent(sender, eventArgs); //ButtonClickedCallback() from MyButton: {ButtonState: DoubleClick}
	/// 		//...
	///   }
	///   </code>
	/// </example>
	public static void TraceEvent(object? sender, EventArgs eventArgs)
	{
		Debug("{CallbackName} from {Sender}: {@EventArgs}", new StackFrame(1).GetMethod(), sender, eventArgs);
	}

	public static void LogVariable<T>(T variable, string? message = null, [CallerArgumentExpression("variable")] string expr = "Unknown Variable")
	{
		if (message is null)
			Verbose("{VariableName}: {Value}", expr, variable);
		else
			Verbose("{VariableName}: {Value}, {Message}", expr, variable, message);
	}
}