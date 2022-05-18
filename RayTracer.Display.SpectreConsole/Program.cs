using RayTracer.Display.SpectreConsole;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Diagnostics;

#if DEBUG
try
{
	#endif
	CommandApp<RunCommand> app = new();
	app.Configure(
			config =>
			{
				config.ValidateExamples()
					.PropagateExceptions();
			}
	);
	return app.Run(args);
	#if DEBUG
}
catch (Exception e)
{
	Debugger.Launch();
	AnsiConsole.WriteException(e);
	Console.WriteLine(e);
	Console.ReadLine();
	return -1;
}
#endif