using JetBrains.Annotations;
using RayTracer.Core.Scenes;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Reflection;

namespace RayTracer.Display.Cli;

[PublicAPI]
internal sealed class RunCommand : Command<RunCommand.Settings>
{
	/// <inheritdoc/>
	public override int Execute(CommandContext context, Settings settings)
	{
		//Print settings to console
		{
			Table table = new()
			{
					Title = new TableTitle("[bold]Provided Options:[/]")
			};
			//Headings for the columns
			table.AddColumn("Option");
			table.AddColumn("Value");

			foreach (PropertyInfo propertyInfo in typeof(Settings).GetProperties())
				table.AddRow(propertyInfo.Name, $"[italic]{propertyInfo.GetValue(settings)?.ToString() ?? "<null>"}[/]");
			AnsiConsole.Write(table);
		}
		//Select scene
		AnsiConsole.MarkupLine("[bold]Please select which scene you wish to load:[/]");
		Scene scene = AnsiConsole.Prompt(
				new SelectionPrompt<Scene>()
						.Title("Title")
						.AddChoices(
								BuiltinScenes.Sphere,
								BuiltinScenes.TwoSpheres
						)
		);
		AnsiConsole.MarkupLine($"Selected scene is [bold]{scene}[/]");

		return 0;
	}

	[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
	internal sealed class Settings : CommandSettings
	{
		[Description("How many pixels wide the image should be")]
		[CommandOption("-w|--width")]
		[DefaultValue(1920)]
		public int Width { get; init; }

		[Description("How many pixels high the image should be")]
		[CommandOption("-h|--height")]
		[DefaultValue(1080)]
		public int Height { get; init; }

		[Description("The output path for the rendered image")]
		[CommandOption("-o|--output|--output-file")]
		[DefaultValue("/image.png")]
		public string OutputFile { get; init; } = null!;
	}
}