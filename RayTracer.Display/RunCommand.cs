using JetBrains.Annotations;
using RayTracer.Core.Debugging;
using RayTracer.Core.Graphics;
using RayTracer.Core.Scenes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

#pragma warning disable CS8765

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
			table.AddColumn("[bold]Option[/]");
			table.AddColumn("[bold]Value[/]");

			foreach (PropertyInfo propertyInfo in typeof(Settings).GetProperties())
				table.AddRow(propertyInfo.Name, $"[italic]{propertyInfo.GetValue(settings)?.ToString() ?? "<null>"}[/]");
			AnsiConsole.Write(table);
		}


		RenderOptions renderOptions = new(settings.Width, settings.Height, settings.KMin, settings.KMax, settings.Threaded, settings.Samples, settings.MaxBounces, settings.DebugVisualisation);
		//Select scene
		Scene scene = AnsiConsole.Prompt(
				new SelectionPrompt<Scene>()
						.Title("[bold]Please select which scene you wish to load:[/]")
						.AddChoices(
								typeof(BuiltinScenes)
										.GetFields(BindingFlags.Public | BindingFlags.Static)
										.Where(f => f.FieldType == typeof(Scene))
										.Select(f => (Scene)f.GetValue(null)!)
						)
		);
		AnsiConsole.MarkupLine($"Selected scene is [italic]{scene}[/]");


		//Render the scene
		AsyncRenderJob renderJob = new(scene, renderOptions);

		//Create a live display that shows what the current image preview is like
		ImageRenderable canvasImage = new(renderJob.Buffer) { MaxWidth = 76, Resampler = new NearestNeighborResampler() };
		AnsiConsole.Live(canvasImage)
					.StartAsync(
							async ctx =>
							{
								while (!renderJob.RenderCompleted)
								{
									ctx.Refresh();
									await Task.Delay(5000);
								}
							}
					);

		Image image = renderJob.GetAwaiter().GetResult();


		//Print any errors
		if (!GraphicsValidator.Errors.IsEmpty)
		{
			ConcurrentDictionary<GraphicsErrorType, ConcurrentDictionary<object, ulong>> errors = GraphicsValidator.Errors;
			AnsiConsole.MarkupLine("[red]Finished rendering with errors[/]");
			//Print a list of all the errors that occurred
			Table table = new()
			{
					Title = new TableTitle("[bold red]Errors occured during render:[/]")
			};
			//I chose to have the error type on the top (column) and the object on the left (row)
			//We have to build a comprehensive list of all possible rows and columns that occur in all dimensions/levels of the dictionaries (not all objects will exist for all error types and vice versa)
			GraphicsErrorType[] allErrorTypes = Enum.GetValues<GraphicsErrorType>(); //All possible enum values for the types of error that can occur
			HashSet<object>     allObjects    = new();                               //Aggregated of all the objects that had any errors (can be just one type of error or all types)

			//Important to create all the columns first, before we create the rows, or we get exceptions (not enough columns)
			table.AddColumn("[bold]Erroring object[/]");
			foreach (GraphicsErrorType errorType in allErrorTypes) table.AddColumn($"[bold]{errorType}[/]");
			//Aggregate all the objects that had errors
			foreach (GraphicsErrorType errorType in allErrorTypes)
			{
				if (!errors.TryGetValue(errorType, out ConcurrentDictionary<object, ulong>? objectMap)) continue;
				foreach (object obj in objectMap.Keys) allObjects.Add(obj);
			}

			//Build the rows. Each row represents the erroring object, and the error counts for it
			var    row            = new IRenderable[allErrorTypes.Length + 1];
			Markup noErrorsMarkup = new("[italic green]N/A[/]");
			foreach (object obj in allObjects)
			{
				Array.Fill(row, new Markup("[bold red]ERROR[/]"));
				row[0] = new Markup(obj.ToString()!);
				//Calculate all the columns (error count values) for the current object. Important that the loop is shifted +1 so that i=0 is the object name
				for (int i = 1; i <= allErrorTypes.Length; i++)
				{
					GraphicsErrorType errorType = allErrorTypes[i - 1];
					//Try get the error count, and if one doesn't exist then we know it's 0
					if (!errors.ContainsKey(errorType))
					{
						row[i] = noErrorsMarkup;
						continue;
					}

					bool exists = errors[errorType].TryGetValue(obj, out ulong count);
					if (!exists || (count == 0)) row[i] = noErrorsMarkup;
					//Change the count text's colour depending on how large the count is
					else
						row[i] = new Markup(
								@$"[{count switch
								{
										< 100   => "#afd700",
										< 500   => "#ffff00",
										< 1000  => "#ffaf00",
										< 2500  => "#ff8700",
										< 5000  => "#ff5f00",
										< 10000 => "#ff2f00",
										_       => "#ff0000"
								}} bold]{count}[/]"
						);
				}

				table.AddRow(row);
			}


			AnsiConsole.Write(table);
		}
		else
		{
			AnsiConsole.MarkupLine("[green]Finished rendering![/]");
		}


		//Save and open the image for viewing
		image.Save(File.OpenWrite(settings.OutputFile), new PngEncoder());
		Process.Start(
				new ProcessStartInfo
				{
						FileName  = "gwenview",
						Arguments = $"\"{settings.OutputFile}\"",
						//These flags stop the image display program's console from attaching to ours (because that's yuck!)
						UseShellExecute        = false,
						RedirectStandardError  = true,
						RedirectStandardInput  = true,
						RedirectStandardOutput = true
				}
		)!.WaitForExit();
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

		// ReSharper disable once StringLiteralTypo
		[Description("Minimum distance along the ray to check for intersections with objects")]
		[CommandOption("--kmin")]
		[DefaultValue(0.001f)]
		public float KMin { get; init; }

		// ReSharper disable once StringLiteralTypo
		[Description("Maximum distance along the ray to check for intersections with objects")]
		[CommandOption("--kmax")]
		[DefaultValue(float.PositiveInfinity)]
		public float KMax { get; init; }

		[Description("The output path for the rendered image")]
		[CommandOption("-o|--output|--output-file")]
		[DefaultValue("./image.png")]
		public string OutputFile { get; init; } = null!;

		//TODO: Make this an int to change how many threads at a time, maybe even modifiable on the fly
		[Description("Flag for enabling multithreaded rendering")]
		[CommandOption("-t|--threaded")]
		[DefaultValue(false)]
		public bool Threaded { get; init; }

		[Description("How many times to sample each pixel")]
		[CommandOption("-s|--samples")]
		[DefaultValue(100)]
		public int Samples { get; init; }

		[Description("Maximum number of times a ray can bounce")]
		[CommandOption("-b|--bounces")]
		[DefaultValue(100)]
		public int MaxBounces { get; init; }

		[Description("Flag for enabling debugging visualisations, such as surface normals")]
		[CommandOption("--debug|--visualise")]
		[DefaultValue(GraphicsDebugVisualisation.None)]
		public GraphicsDebugVisualisation DebugVisualisation { get; init; }
	}
}