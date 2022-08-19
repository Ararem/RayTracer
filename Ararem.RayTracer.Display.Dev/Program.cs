using Ararem.RayTracer.Core;
using Ararem.RayTracer.Display.Dev.Resources;
using Ararem.RayTracer.Display.Dev.UI;
using Eto;
using Eto.Forms;
using LibArarem.Core.Logging;
using Serilog;
using Serilog.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Figgle.FiggleFonts;
using static Ararem.RayTracer.Display.Dev.Program.ExitCode;
using static Serilog.Log;
using Logger = Ararem.RayTracer.Core.Logger;
using UnhandledExceptionEventArgs = System.UnhandledExceptionEventArgs;

namespace Ararem.RayTracer.Display.Dev;

/// <summary>Bootstrap class that contains the <c>Main()</c> function that inits everything else.</summary>
internal static class Program
{
	public enum ExitCode
	{
		/// <summary>The exit was purposeful (e.g. the user clicked the quit button), and not due to exceptional circumstances</summary>
		GracefulExit = 00,

		/// <summary>The app failed during the initialization stage because it could not set up the <see cref="AppDomain"/> exception handlers</summary>
		/// <seealso cref="AppDomain.UnhandledException"/>
		/// <seealso cref="AppDomain.FirstChanceException"/>
		InitFail_CouldNotSetAppDomainExceptionHandlers = 01,

		/// <summary>The app failed during the initialization stage because it could not initialise the <see cref="TaskWatcher">Task Watcher</see>
		/// </summary>
		/// <seealso cref="Platform.Detect"/>
		InitFail_CouldNotInitTaskWatcher = 02,

		/// <summary>The app failed during the initialization stage because it could not set up the <see cref="Platform"/></summary>
		/// <seealso cref="Platform.Detect"/>
		InitFail_CouldNotInitPlatform = 03,

		/// <summary>The app failed during the initialization stage because it could not create the <see cref="Application"/> object</summary>
		/// <seealso cref="Platform.Detect"/>
		InitFail_CouldNotCreateApplicationObject = 04,

		/// <summary>The app failed during the initialization stage because it could not set the event handler for ETO unhandled exceptions</summary>
		/// <seealso cref="Application.UnhandledException"/>
		InitFail_CouldNotSetEtoExceptionHandler = 05,

		/// <summary>The app failed during the initialization stage because it was unable to initialize the manager classes</summary>
		/// <seealso cref="StyleManager"/>
		/// <seealso cref="ResourceManager"/>
		InitFail_CouldNotInitManagerClasses = 06,

		/// <summary>The app failed during the initialization stage because it was unable to create a <see cref="MainForm"/> instance</summary>
		/// <seealso cref="MainForm"/>
		InitFail_CouldNotCreateMainForm = 07,

		/// <summary>App failed while running</summary>
		AppFailure = 08,

		/// <summary>
		/// In internal error was thrown that could not/was not handled, and the application fatally exited
		/// </summary>
		UncaughtInternalError = 101,

	}

	private static string Title             => AssemblyInfo.ProductName;
	private static string AppTitleVersioned => $"{Title} - v {AssemblyInfo.Version}";

	[SuppressMessage("ReSharper.DPA", "DPA0001: Memory allocation issues")] //Mainly due to Eto.Forms doing it's own thing
	[STAThread]
	private static int Main(string[] args)
	{
		ExitCode exitCode;
		try
		{
			exitCode = MainInternal(args);
		}
		catch (Exception mainException)
		{
			//Yes it's dumb, but there have been occasions (rarely) where this has thrown...
			//Often due to file not found errors with the Ben.Demystifier assembly
			try
			{
				Console.Error.WriteLine(mainException.ToStringDemystified());
			}
			catch (Exception demystifyException)
			{
				Console.Error.WriteLine(
						$@"Could not demystify exception:
{demystifyException}

Original exception:
{mainException}"
				);
			}

			exitCode = UncaughtInternalError;
		}

		int      intCode = (int)exitCode;
		new StreamWriter(Console.OpenStandardOutput()).WriteLine("Exit code: {0} ({1})",Enum.GetName(exitCode), intCode); //Print exit code
		return intCode;
	}

	[SuppressMessage("ReSharper.DPA", "DPA0001: Memory allocation issues")] //Mainly due to Eto.Forms doing it's own thing
	private static ExitCode MainInternal(string[] args)
	{
		//Figgle banner, most important part of the program
		{
			/*
			List<FiggleFont> figgleFonts =new()
			{
					//Commented fonts are too big :(
					// FiveLineOblique,
					// Big,
					// Colossal,
					// Doh,
					// Epic,
					Ivrit,
					// Starwars,
					// Univers
			};
			*/
			//Very important, whole program crashes without this Console.WriteLine(), idk y
			string paddedTitle = string.Join(' ', (IEnumerable<char>)AppTitleVersioned); //Append a space between each char, or it's too crammed
			Console.ForegroundColor = ConsoleColor.Blue;
			Console.WriteLine(Ivrit.Render(paddedTitle));
		}

		//Console and init
		#if DEBUG
		Console.WriteLine($"{Title}: Starting program");
		try
		{
			Console.Title = $"{AppTitleVersioned} [Console]";
		}
		catch (Exception)
		{
			// ignored - title isn't important if it fails
		}

		Console.WriteLine($"{Title}: Initialising logger");
		Console.ResetColor();
		Console.SetError(new ColouredConsoleErrorWriter(Console.Error));

		#endif

		Logger.Init(EarlyAdjustConfig);
		LogAggregator.Init();
		if(Console.OpenStandardOutput() == Stream.Null) Warning("No console standard IO streams found");

		Information("Commandline args: {Args}", args);
		Information("{AppTitle} starting",      AssemblyInfo.ProductName);
		Debug("CLR Version: {Value}",       Environment.Version);
		Debug("Current Directory: {Value}", Environment.CurrentDirectory);
		Debug("OS Version: {Value}",        Environment.OSVersion);
		Debug("Environment Variables:");
		foreach (DictionaryEntry variable in Environment.GetEnvironmentVariables())
		{
			Debug<object, object?>("{Variable} = \"{Value}\"", variable.Key, variable.Value);
		}

		Information("Initialising app components");
		try
		{
			AppDomain domain = AppDomain.CurrentDomain;
			Debug("Setting up AppDomain exception catchers for CurrentDomain={AppDomain}", domain);
			UnhandledExceptionEventHandler unhandledExceptionEventHandler = CurrentDomainOnUnhandledException;
			domain.UnhandledException += unhandledExceptionEventHandler;
			EventHandler<FirstChanceExceptionEventArgs> firstChangeExceptionEventHandler = FirstChanceException;
			domain.FirstChanceException += firstChangeExceptionEventHandler;
			Debug("Set up AppDomain exception catchers");
		}
		catch (Exception e)
		{
			Fatal(e, "Could not set up app domain exception handlers");
			return InitFail_CouldNotSetAppDomainExceptionHandlers;
		}

		try
		{
			Debug("Starting task watcher");
			TaskWatcher.Init();
			Debug("Task watcher started");
		}
		catch (Exception e)
		{
			Fatal(e, "Failed to initialize task watcher");
			return InitFail_CouldNotInitTaskWatcher;
		}

		Platform platform;
		try
		{
			Debug("Getting platform (Detect Mode)");
			platform = Platform.Detect!;
			Debug("Got Platform: {Platform}", platform);
		}
		catch (Exception e)
		{
			Fatal(e, "Could not initialise Eto.Forms platform");
			return InitFail_CouldNotInitPlatform;
		}

		Application application;
		try
		{
			Debug("Creating new application object");
			application = new Application(platform)
			{
					Name              = "RayTracer.Display",
					ID                = "Main Application",
					UIThreadCheckMode = UIThreadCheckMode.Error,
					BadgeLabel        = "RayTracer [Badge]"
			};
			Debug("Created application object: {Application}", application);
		}
		catch (Exception e)
		{
			Fatal(e, "Could not initialise Eto.Forms application");
			return InitFail_CouldNotCreateApplicationObject;
		}

		try
		{
			Debug("Hooking up ETO.Application unhandled exception event");
			EventHandler<Eto.UnhandledExceptionEventArgs> unhandledExceptionHandler = EtoUnhandledException;
			application.UnhandledException += unhandledExceptionHandler;
			Debug("ETO.Application unhandled exception event handler added: {@Handler}", unhandledExceptionHandler);
		}
		catch (Exception e)
		{
			Fatal(e, "Failed to set up unhandled exception handler");
			return InitFail_CouldNotSetEtoExceptionHandler;
		}

		try
		{
			Debug("Initialising manager classes");
			RuntimeHelpers.RunClassConstructor(typeof(StyleManager).TypeHandle);
			RuntimeHelpers.RunClassConstructor(typeof(ResourceManager).TypeHandle);
			Debug("Initialised manager classes");
		}
		catch (Exception e)
		{
			Fatal(e, "Could not init manager classes");
			return InitFail_CouldNotInitManagerClasses;
		}

		MainForm mainForm;
		try
		{
			Debug("Creating new MainForm");
			mainForm = new MainForm
			{
					ID = "MainForm"
			};
			Debug("Created new MainForm: {MainForm}", mainForm);
		}
		catch (Exception e)
		{
			Fatal(e, "Could not initialise MainForm");
			return InitFail_CouldNotCreateMainForm;
		}

		try
		{
			Information("Running App");
			application.Run(mainForm);
			Information("App ran to completion");
			return GracefulExit;
		}
		catch (Exception e)
		{
			Fatal(e, "App threw exception");
			return AppFailure;
		}
		finally
		{
			Information("Performing cleanup");
			//Not sure what exactly needs to be disposed, but I'll do it all just to be sure since I did get some warns about not disposed code from GLib before
			try
			{
				Debug("Disposing application objects and quitting");
				Verbose($"Disposing {nameof(mainForm)}");
				mainForm.Dispose();
				Verbose($"Disposing {nameof(application)}");
				application.Dispose();
				Debug("Disposed app and main form");
			}
			catch (Exception e)
			{
				Error(e, "Could not dispose application");
			}

			Information("Shutting down logger and exiting");
			CloseAndFlush();
			#if DEBUG
			Console.WriteLine("Cleanup complete");
			Console.WriteLine("Goodbye!");
			#endif
		}
	}

	private static LoggerConfiguration EarlyAdjustConfig(LoggerConfiguration arg)
	{
		return arg
			    .Filter.ByExcluding(evt => (evt.Level == LogEventLevel.Verbose) && evt.Properties.ContainsKey(nameof(LogUtils.MarkContextAsExtremelyVerbose)))
			   .Destructure.ByTransformingWhere(static t => t.IsAssignableTo(typeof(Widget)), static (Widget w) => !w.IsDisposed? w.ID : "(<Disposed>)") //Transform widgets into their ID
			   .Destructure.ByTransforming<Command>(static c => string.IsNullOrEmpty(c.ID) ? $"{c} (<unnamed>)" : $"{c} ({c.ID})");
	}

	private static void OnUnhandledException(Exception exception, object? sender, bool isTerminating)
	{
		Write(
				isTerminating ? LogEventLevel.Fatal : LogEventLevel.Error, exception, "Caught unhandled exception ({Terminating}) from {Sender}:",
				isTerminating ? "terminating" : "non-terminating",         sender
		);
	}

	private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		OnUnhandledException((Exception)e.ExceptionObject, sender, e.IsTerminating);
	}

	/*
	 * First-chance exceptions are often fine, since they'll be caught most of the time,
	 * but sometimes async exceptions can get hidden and suppressed, despite being uncaught AND are missed by AppDomain.UnhandledException
	 * So log them just in case, but they're unimportant hence the verbose
	 */
	private static void FirstChanceException(object? sender, FirstChanceExceptionEventArgs eventArgs)
	{
		Debug(eventArgs.Exception, "First-chance exception from {Sender}:", propertyValue: sender);
	}

	private static void EtoUnhandledException(object? sender, Eto.UnhandledExceptionEventArgs e)
	{
		OnUnhandledException((Exception)e.ExceptionObject, sender, e.IsTerminating);
	}

	/// <summary>Makes the Console.Error stuff a different colour...</summary>
	private sealed class ColouredConsoleErrorWriter : TextWriter
	{
		private const string ColourString = "\x1b[1m\x1b[4m\x1b[31m";

		private readonly TextWriter textWriterImplementation;

		/// <inheritdoc/>
		public ColouredConsoleErrorWriter(TextWriter textWriterImplementation)
		{
			this.textWriterImplementation = textWriterImplementation;
		}

		public override Encoding Encoding => textWriterImplementation.Encoding;

		public override IFormatProvider FormatProvider => textWriterImplementation.FormatProvider;

		public override string NewLine
		{
			get => textWriterImplementation.NewLine;
			#pragma warning disable CS8765
			set => textWriterImplementation.NewLine = value;
			#pragma warning restore CS8765
		}

		private void WriteColour()
		{
			textWriterImplementation.Write(ColourString);
		}

		public override void Close()
		{
			textWriterImplementation.Close();
		}

		public override ValueTask DisposeAsync() => textWriterImplementation.DisposeAsync();

		public override void Flush()
		{
			textWriterImplementation.Flush();
		}

		public override Task FlushAsync() => textWriterImplementation.FlushAsync();

		public override void Write(bool value)
		{
			WriteColour();
			textWriterImplementation.Write(value);
		}

		public override void Write(char value)
		{
			WriteColour();
			textWriterImplementation.Write(value);
		}

		public override void Write(char[]? buffer)
		{
			WriteColour();
			textWriterImplementation.Write(buffer);
		}

		public override void Write(char[] buffer, int index, int count)
		{
			WriteColour();
			textWriterImplementation.Write(buffer, index, count);
		}

		public override void Write(decimal value)
		{
			WriteColour();
			textWriterImplementation.Write(value);
		}

		public override void Write(double value)
		{
			WriteColour();
			textWriterImplementation.Write(value);
		}

		public override void Write(int value)
		{
			WriteColour();
			textWriterImplementation.Write(value);
		}

		public override void Write(long value)
		{
			WriteColour();
			textWriterImplementation.Write(value);
		}

		public override void Write(object? value)
		{
			WriteColour();
			textWriterImplementation.Write(value);
		}

		public override void Write(ReadOnlySpan<char> buffer)
		{
			WriteColour();
			textWriterImplementation.Write(buffer);
		}

		public override void Write(float value)
		{
			WriteColour();
			textWriterImplementation.Write(value);
		}

		public override void Write(string? value)
		{
			WriteColour();
			textWriterImplementation.Write(value);
		}

		public override void Write(string format, object? arg0)
		{
			WriteColour();
			textWriterImplementation.Write(format, arg0);
		}

		public override void Write(string format, object? arg0, object? arg1)
		{
			WriteColour();
			textWriterImplementation.Write(format, arg0, arg1);
		}

		public override void Write(string format, object? arg0, object? arg1, object? arg2)
		{
			WriteColour();
			textWriterImplementation.Write(format, arg0, arg1, arg2);
		}

		public override void Write(string format, params object?[] arg)
		{
			WriteColour();
			textWriterImplementation.Write(format, arg);
		}

		public override void Write(StringBuilder? value)
		{
			WriteColour();
			textWriterImplementation.Write(value);
		}

		public override void Write(uint value)
		{
			WriteColour();
			textWriterImplementation.Write(value);
		}

		public override void Write(ulong value)
		{
			WriteColour();
			textWriterImplementation.Write(value);
		}

		public override Task WriteAsync(char value) => textWriterImplementation.WriteAsync(value);

		public override Task WriteAsync(char[] buffer, int index, int count) => textWriterImplementation.WriteAsync(buffer, index, count);

		public override Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = new()) => textWriterImplementation.WriteAsync(buffer, cancellationToken);

		public override Task WriteAsync(string? value) => textWriterImplementation.WriteAsync(value);

		public override Task WriteAsync(StringBuilder? value, CancellationToken cancellationToken = new()) => textWriterImplementation.WriteAsync(value, cancellationToken);

		public override void WriteLine()
		{
			WriteColour();
			textWriterImplementation.WriteLine();
		}

		public override void WriteLine(bool value)
		{
			WriteColour();
			textWriterImplementation.WriteLine(value);
		}

		public override void WriteLine(char value)
		{
			WriteColour();
			textWriterImplementation.WriteLine(value);
		}

		public override void WriteLine(char[]? buffer)
		{
			WriteColour();
			textWriterImplementation.WriteLine(buffer);
		}

		public override void WriteLine(char[] buffer, int index, int count)
		{
			WriteColour();
			textWriterImplementation.WriteLine(buffer, index, count);
		}

		public override void WriteLine(decimal value)
		{
			WriteColour();
			textWriterImplementation.WriteLine(value);
		}

		public override void WriteLine(double value)
		{
			WriteColour();
			textWriterImplementation.WriteLine(value);
		}

		public override void WriteLine(int value)
		{
			WriteColour();
			textWriterImplementation.WriteLine(value);
		}

		public override void WriteLine(long value)
		{
			WriteColour();
			textWriterImplementation.WriteLine(value);
		}

		public override void WriteLine(object? value)
		{
			WriteColour();
			textWriterImplementation.WriteLine(value);
		}

		public override void WriteLine(ReadOnlySpan<char> buffer)
		{
			WriteColour();
			textWriterImplementation.WriteLine(buffer);
		}

		public override void WriteLine(float value)
		{
			WriteColour();
			textWriterImplementation.WriteLine(value);
		}

		public override void WriteLine(string? value)
		{
			WriteColour();
			textWriterImplementation.WriteLine(value);
		}

		public override void WriteLine(string format, object? arg0)
		{
			WriteColour();
			textWriterImplementation.WriteLine(format, arg0);
		}

		public override void WriteLine(string format, object? arg0, object? arg1)
		{
			WriteColour();
			textWriterImplementation.WriteLine(format, arg0, arg1);
		}

		public override void WriteLine(string format, object? arg0, object? arg1, object? arg2)
		{
			WriteColour();
			textWriterImplementation.WriteLine(format, arg0, arg1, arg2);
		}

		public override void WriteLine(string format, params object?[] arg)
		{
			WriteColour();
			textWriterImplementation.WriteLine(format, arg);
		}

		public override void WriteLine(StringBuilder? value)
		{
			WriteColour();
			textWriterImplementation.WriteLine(value);
		}

		public override void WriteLine(uint value)
		{
			WriteColour();
			textWriterImplementation.WriteLine(value);
		}

		public override void WriteLine(ulong value)
		{
			WriteColour();
			textWriterImplementation.WriteLine(value);
		}

		public override Task WriteLineAsync()
		{
			WriteColour();
			return textWriterImplementation.WriteLineAsync();
		}

		public override Task WriteLineAsync(char value)
		{
			WriteColour();
			return textWriterImplementation.WriteLineAsync(value);
		}

		public override Task WriteLineAsync(char[] buffer, int index, int count)
		{
			WriteColour();
			return textWriterImplementation.WriteLineAsync(buffer, index, count);
		}

		public override Task WriteLineAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = new())
		{
			WriteColour();
			return textWriterImplementation.WriteLineAsync(buffer, cancellationToken);
		}

		public override Task WriteLineAsync(string? value)
		{
			WriteColour();
			return textWriterImplementation.WriteLineAsync(value);
		}

		public override Task WriteLineAsync(StringBuilder? value, CancellationToken cancellationToken = new())
		{
			WriteColour();
			return textWriterImplementation.WriteLineAsync(value, cancellationToken);
		}
	}
}