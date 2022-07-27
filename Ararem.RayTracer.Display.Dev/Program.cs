using Ararem.RayTracer.Core;
using Ararem.RayTracer.Display.Dev.Resources;
using Eto;
using Eto.Forms;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Figgle.FiggleFonts;
using static Serilog.Log;
using static Ararem.RayTracer.Display.Dev.Program.ExitCode;
using Logger = Ararem.RayTracer.Core.Logger;
using UnhandledExceptionEventArgs = System.UnhandledExceptionEventArgs;

namespace Ararem.RayTracer.Display.Dev;

/// <summary>Bootstrap class that contains the <c>Main()</c> function that inits everything else.</summary>
internal static class Program
{
	public enum ExitCode
	{
		/// <summary>The exit was purposeful (e.g. the user clicked the quit button), and not due to exceptional circumstances</summary>
		GracefulExit = 0,

		/// <inheritdoc cref="GracefulExit"/>
		None = GracefulExit,

		/// <summary>The app failed during the initialization stage</summary>
		InitializationFailure,

		/// <summary>App failed while running</summary>
		AppFailure
	}

	private static string Title             => AssemblyInfo.ProductName;
	private static string AppTitleVersioned => $"{Title} - v {AssemblyInfo.Version}";

	[SuppressMessage("ReSharper.DPA", "DPA0001: Memory allocation issues")] //Mainly due to Eto.Forms doing it's own thing
	private static int Main(string[] args) => (int)MainInternal(args);

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
		Console.Title = $"{AppTitleVersioned} [Console]";
		Console.WriteLine($"{Title}: Initialising logger");
		#endif
		Logger.Init();

		Information("Starting {AppTitle} app", AssemblyInfo.ProductName);
		Information("Commandline args: {Args}", args);

		try
		{
			Debug("Setting up AppDomain exception catchers");
			AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

			/*
			 * First-chance exceptions are often fine, since they'll be caught most of the time,
			 * but sometimes async exceptions can get hidden and suppressed, despite being uncaught AND are missed by AppDomain.UnhandledException
			 * So log them just in case, but they're unimportant hence the verbose
			 */
			void FirstChanceException(object? sender, FirstChanceExceptionEventArgs eventArgs)
			{
				Verbose(eventArgs.Exception, "First-chance exception from {Sender}:", sender);
			}

			AppDomain.CurrentDomain.FirstChanceException += FirstChanceException;
			Debug("Set up AppDomain exception catchers");
		}
		catch (Exception e)
		{
			Fatal(e, "Could not set up app domain exception handlers");
			return InitializationFailure;
		}

		Platform platform;
		try
		{
			Debug("Getting platform (Detect Mode)");
			platform = Platform.Detect!;
			Debug("Got Platform: {Platform}", platform.ID);
		}
		catch (Exception e)
		{
			Fatal(e, "Could not initialise Eto.Forms platform");
			return InitializationFailure;
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
			return InitializationFailure;
		}

		try
		{
			Debug("Hooking up unhandled exception event");
			EventHandler<Eto.UnhandledExceptionEventArgs> unhandledExceptionHandler = EtoUnhandledException;
			application.UnhandledException += unhandledExceptionHandler;
			Debug("Unhandled exception event handler added: {@Handler}", unhandledExceptionHandler);
		}
		catch (Exception e)
		{
			Fatal(e, "Failed to set up unhandled exception handler");
			return InitializationFailure;
		}

		Console.SetError(new ColouredConsoleErrorWriter(Console.Error));

		try
		{
			Information("Initialising manager classes");
			RuntimeHelpers.RunClassConstructor(typeof(StyleManager).TypeHandle);
			RuntimeHelpers.RunClassConstructor(typeof(ResourceManager).TypeHandle);
			Information("Initialised manager classes");
		}
		catch (Exception e)
		{
			Fatal(e, "Could not init manager classes");
			return InitializationFailure;
		}

		MainForm mainForm;
		try
		{
			Debug("Creating new MainForm");
			mainForm = new MainForm();
			Debug("Created new MainForm: {MainForm}", mainForm);
		}
		catch (Exception e)
		{
			Fatal(e, "Could not initialise MainForm");
			return InitializationFailure;
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
			return InitializationFailure;
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

	private static void OnUnhandledException(Exception exception, object? sender, bool isTerminating)
	{
		Error(exception, "Caught unhandled exception ({Terminating}) from {Sender}:", isTerminating ? "terminating" : "non-terminating", sender);
	}

	private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		OnUnhandledException((Exception)e.ExceptionObject, sender, e.IsTerminating);
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