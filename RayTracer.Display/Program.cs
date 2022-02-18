using RayTracer.Display;
using Spectre.Console.Cli;

var app = new CommandApp<RunCommand>();
app.Configure(
		config =>
		{
			config.ValidateExamples()
				.PropagateExceptions();
		}
);
return app.Run(args);