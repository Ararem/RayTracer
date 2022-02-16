using JetBrains.Annotations;
using RayTracer.Core.Debugging;
using RayTracer.Core.Graphics;
using RayTracer.Core.Scenes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using Color = Spectre.Console.Color;

#pragma warning disable CS8765

namespace RayTracer.Display.Cli;

[PublicAPI]
[NoReorder]
internal sealed class RunCommand : Command<RunCommand.Settings>
{
	/// <summary>
	///  Displays and confirms the settings passed in by the user
	/// </summary>
	/// <returns>The confirmed scene and render options, to use for render execution</returns>
	private static (Scene Scene, RenderOptions Options) ConfirmSettings(CommandContext context, Settings settings)
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
		return (scene, renderOptions);
	}

	/// <summary>
	///  Creates a little live display for while the render is running
	/// </summary>
	private static void DisplayProgress(AsyncRenderJob renderJob)
	{
		//By using an outer loop, we can make the console easier to modify realtime while debugging
		while (!renderJob.RenderCompleted)
		{
			//Don't update as fast as possible or we get flickering
			Thread.Sleep(2000);
			AnsiConsole.Clear();

			//TODO: Update console window title

			//The outermost table that just splits the render stats from the image preview
			Table statsAndImageTable = new Table
			{
					Border = new DoubleTableBorder(), BorderStyle = new Style(Color.Red),
					Title  = new TableTitle($"[bold red underline]RayTracer v{typeof(Scene).Assembly.GetName().Version} - {renderJob.Scene.Name}[/]")
			}.AddColumns("Render Statistics", "Image Preview");

			//TODO: Fix the width so it's a bit better (auto adjust?)
			ImageRenderable imagePreviewRenderable = new(renderJob.ImageBuffer)
			{
					// MaxWidth = 76,
					Resampler = new NearestNeighborResampler()
			};

			Table renderStatsTable = new Table
			{
					Border = new DoubleTableBorder(), BorderStyle = new Style(Color.Blue),
					Title  = new TableTitle("[bold blue underline]Render Statistics[/]")
			}.AddColumns("Property", "Value").HideHeaders(); //Add the headers so the count is correct, but we don't want them shown

			statsAndImageTable.AddRow(renderStatsTable, imagePreviewRenderable);

			ulong totalRawPixels  = renderJob.TotalRawPixels;
			ulong totalTruePixels = renderJob.TotalTruePixels;

			float    percentageRendered    = (float)renderJob.RawPixelsRendered / totalRawPixels;
			float    rawPixelsRemaining    = totalRawPixels - renderJob.RawPixelsRendered,        truePixelsRemaining = totalTruePixels - renderJob.TruePixelsRendered;
			float    percentageRaw         = (float)renderJob.RawPixelsRendered / totalRawPixels, percentageTrue      = (float)renderJob.TruePixelsRendered / totalTruePixels;
			TimeSpan elapsed               = renderJob.Stopwatch.Elapsed;
			TimeSpan estimatedTotalTime    = elapsed / percentageRendered;
			ulong    rayCount              = renderJob.RayCount;
			float    rayScatterPercentage  = (float)renderJob.RaysScattered       / rayCount;
			float    rayAbsorbedPercentage = (float)renderJob.RaysAbsorbed        / rayCount;
			float    rayExceededPercentage = (float)renderJob.BounceLimitExceeded / rayCount;
			float    skyRayPercentage      = (float)renderJob.SkyRays             / rayCount;

			//TODO: Progress..?
			const string time    = "h\\:mm\\:ss"; //Format string for timespan
			const string percent = "p1";          //Format string for percentages
			const string num     = "n0";
			renderStatsTable.AddRow("Time",                 $"{elapsed.ToString(time)} elapsed, {(estimatedTotalTime - elapsed).ToString(time)} remaining, {estimatedTotalTime.ToString(time)} total");
			renderStatsTable.AddRow("Raw Pixels Rendered",  $"{renderJob.RawPixelsRendered.ToString(num)} ({percentageRaw.ToString(percent)}) rendered, {rawPixelsRemaining.ToString(num)} remaining, {totalRawPixels.ToString(num)} total");
			renderStatsTable.AddRow("True Pixels Rendered", $"{renderJob.TruePixelsRendered.ToString(num)} ({percentageTrue.ToString(percent)}) rendered, {truePixelsRemaining.ToString(num)} remaining, {totalTruePixels.ToString(num)} total");
			renderStatsTable.AddRow("Rays",                 $"{renderJob.RaysScattered.ToString(num)} ({rayScatterPercentage.ToString(percent)}) scattered, {renderJob.RaysAbsorbed.ToString(num)} ({rayAbsorbedPercentage.ToString(percent)}) absorbed, {renderJob.BounceLimitExceeded.ToString(num)} ({rayExceededPercentage.ToString(percent)}) exceeded, {renderJob.SkyRays.ToString(num)} ({skyRayPercentage.ToString(percent)}) sky, {rayCount.ToString(num)} total");
			renderStatsTable.AddRow("Depth Buffer",         "[bold italic red]Coming soon...[/]");

			AnsiConsole.Write(statsAndImageTable);
		}
	}

	/// <summary>
	///  Function to be called once a render job is finished. Also returns the rendered image
	/// </summary>
	private static Image<Rgb24> FinalizeRenderJob(AsyncRenderJob renderJob)
	{
		Image<Rgb24> image = renderJob.GetAwaiter().GetResult();

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
				//Fill the array with error messages so that I can tell when i mess up, also saves the app from crashing if that happens (because the default is null)
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

		return image;
	}

	/// <inheritdoc/>
	public override int Execute(CommandContext context, Settings settings)
	{
		//Get the settings for how and what we'll render
		(Scene scene, RenderOptions renderOptions) = ConfirmSettings(context, settings);

		//Start the render job and display the progress while we wait
		AsyncRenderJob renderJob = new(scene, renderOptions);
		DisplayProgress(renderJob);

		//Finalize everything
		Image<Rgb24> image = FinalizeRenderJob(renderJob);

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