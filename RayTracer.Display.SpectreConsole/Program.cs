﻿using RayTracer.Display.SpectreConsole;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Diagnostics;

#if DEBUG
try
{
	#endif
	var app = new CommandApp<RunCommand>();
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
catch (NullReferenceException e)
{
	Debugger.Launch();
	AnsiConsole.WriteException(e);
	Console.WriteLine(e);
	Console.ReadKey();
	return -1;
}
#endif