using JetBrains.Annotations;
using RayTracer.Core.Graphics;
using RayTracer.Core.Scenes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics;
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
		Scene scene = AnsiConsole.Prompt(
				new SelectionPrompt<Scene>()
						.Title("[bold]Please select which scene you wish to load:[/]")
						.AddChoices(
								BuiltinScenes.Sphere,
								BuiltinScenes.TwoSpheres
						)
		);
		AnsiConsole.MarkupLine($"Selected scene is [bold]{scene}[/]");

		Image image = Renderer.Render(scene);
		image.Save(File.OpenWrite(settings.OutputFile), new PngEncoder());
		Process.Start("gwenview", $"\"{settings.OutputFile}\"").WaitForExit();
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
		[DefaultValue("./image.png")]
		public string OutputFile { get; init; } = null!;
	}
}