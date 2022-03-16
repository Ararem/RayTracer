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
using System.Text;
using Color = Spectre.Console.Color;

#pragma warning disable CS8765

namespace RayTracer.Display.SpectreConsole;

[PublicAPI]
[NoReorder]
internal sealed class RunCommand : AsyncCommand<RunCommand.Settings>
{
#region Markup Styles

	/// <summary>
	///  Most important - style for the app title text
	/// </summary>
	private const string AppTitleMarkup = "bold red underline";

	/// <summary>
	///  Markup for the title of an <see cref="IRenderable"/>
	/// </summary>
	private const string TitleMarkup = "bold blue";

	/// <summary>
	///  Markup for the heading of a table
	/// </summary>
	private const string HeadingMarkup = "italic blue";

	/// <summary>
	///  Markup for when displaying a scene name/selection
	/// </summary>
	private const string SceneMarkup = "italic";

	/// <summary>
	///  Markup for the "Rendering..." animation
	/// </summary>
	private const string RenderingAnimationMarkup = "italic green";

	private const string StatsCategoryMarkup  = "bold";
	private const string FinishedRenderMarkup = "bold underline";

#endregion

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
					Title = new TableTitle($"[{TitleMarkup}]Provided Options:[/]")
			};
			//Headings for the columns
			table.AddColumn($"[{HeadingMarkup}]Option[/]");
			table.AddColumn($"[{HeadingMarkup}]Value[/]");

			foreach (PropertyInfo propertyInfo in typeof(Settings).GetProperties())
				table.AddRow(propertyInfo.Name, $"[italic]{propertyInfo.GetValue(settings)?.ToString() ?? "<null>"}[/]");
			AnsiConsole.Write(table);
		}


		RenderOptions renderOptions = new(settings.Width, settings.Height, settings.KMin, settings.KMax, settings.Threaded, settings.Samples, settings.MaxBounces, settings.DebugVisualisation);
		//Select scene
		Scene scene = AnsiConsole.Prompt(
				new SelectionPrompt<Scene>()
						.Title($"[{TitleMarkup}]Please select which scene you wish to load:[/]")
						.AddChoices(BuiltinScenes.GetAll())
						.UseConverter(s => $"[{SceneMarkup}]{s}[/]")
		);
		AnsiConsole.MarkupLine($"Selected scene is [{SceneMarkup}]{scene}[/]");
		return (scene, renderOptions);
	}

	/// <summary>
	///  Creates a little live display for while the render is running
	/// </summary>
	private static async Task DisplayProgress(AsyncRenderJob renderJob)
	{
		AnsiConsole.Clear();

		const int interval = 500; //How long between updates of the live display

		//First thing is the title
		string appTitle = $"[{AppTitleMarkup}]RayTracer v{typeof(Scene).Assembly.GetName().Version} - [{SceneMarkup}]{renderJob.Scene.Name}[/][/]";
		Console.Title = Markup.Remove(appTitle);

		//The outermost table that just splits the render stats from the image preview
		Table statsAndImageTable;

		//The outer loop allows us to 'reset' the console, fixing any size issues
		//We use a live display and an inner loop, as clearing and recreating the table causes really bad flickering
		while (true)
		{
			(int W, int H) prevDims = (Console.WindowWidth, Console.WindowHeight);
			AnsiConsole.Clear();
			await AnsiConsole.Live(new Markup("[bold red slowblink]Live Display Starting...[/]")).StartAsync(
					async ctx =>
					{
						//Create a new table (reset)
						statsAndImageTable = new Table { Border = new NoTableBorder(), Title = new TableTitle(appTitle), Alignment = Justify.Center };
						statsAndImageTable.AddColumns(
								new TableColumn($"[{HeadingMarkup}]Render Statistics[/]\n").Centered(),
								new TableColumn($"[{HeadingMarkup}]Image Preview[/]\n").Centered()
						);
						ctx.UpdateTarget(statsAndImageTable);

						//Inner loop doesn't flicker (gasp)
						while (!renderJob.RenderCompleted)
						{
							//Automatically reset if dimensions changed
							(int W, int H) currDims = (Console.WindowWidth, Console.WindowHeight);
							if (prevDims != currDims) break;
							UpdateLiveDisplay();
							ctx.Refresh();
							//await Task.Delay(interval);
							//Allow for a manual reset using the 'C' key
							if (Console.KeyAvailable && (Console.ReadKey(true).Key == ConsoleKey.C)) break;
						}
					}
			);
			//Quit if the render is done
			if (renderJob.RenderCompleted) break;
		}

		void UpdateLiveDisplay()
		{
			statsAndImageTable.Rows.Clear();

		#region Rendering... animation

			StringBuilder sb = new(100);
			const double  f  = 2.5; //Total time per ellipsis cycle (s)
			const double  a  = 5;   //Max ellipses per cycle

			int    n;
			double sec = renderJob.Stopwatch.Elapsed.TotalSeconds;
			#if true
			//Triangle wave, goes up and down
			{
				double sin    = Math.Sin((sec / f) * Math.PI);
				double inv    = Math.Asin(sin);
				double abs    = Math.Abs(inv);
				double scaled = ((abs * a) / Math.PI) * 2;
				n = (int)Math.Round(scaled);
			}
			#else
			//Sawtooth wave
			{
				//Get fractional part of the
				double frac = (sec / f) % 1;
				double scaled = frac      * (a + 1);
				n = (int)Math.Floor(scaled);
			}
			#endif
			sb.Clear();
			sb.Append($"[{RenderingAnimationMarkup}]");
			//Pad/centre string
			sb.Append(' ', n);
			sb.Append("Rendering");
			sb.Append('.', n);
			sb.Append("[/]");

			statsAndImageTable.Caption = new TableTitle(sb.ToString());

		#endregion

		#region Image buffer display

			//The image that shows the current render buffer
			//Make sure we don't exceed the vertical space limit when trying to maximise the width
			//TODO: These sizing thingies don't really work too well on some resolutions
			int                   maxHeight       = Console.WindowHeight - 5; //The offset is so that we leave enough room for the title (1) + heading (2) + caption (1) + newline (1) = 5
			float                 aspect          = (float)renderJob.ImageBuffer.Width / renderJob.ImageBuffer.Height;
			int                   maxWidth        = (int)(maxHeight * aspect);
			CustomImageRenderable imageRenderable = new(renderJob.ImageBuffer) { Resampler = CubicResampler.RobidouxSharp, MaxConsoleWidth = maxWidth };

		#endregion

		#region Render stats table

			Table renderStatsTable = new Table { Border = new DoubleTableBorder(), BorderStyle = new Style(Color.Blue) }.AddColumns($"[{HeadingMarkup}]Property[/]", $"[{HeadingMarkup}]Value[/]").HideHeaders(); //Add the headers so the column count is correct, but we don't want them shown

			int      totalTruePixels = renderJob.TotalTruePixels;
			ulong    totalRawPix     = renderJob.TotalRawPixels;
			ulong    rayCount        = renderJob.RayCount;
			int      totalPasses     = renderJob.RenderOptions.Passes;
			TimeSpan elapsed         = renderJob.Stopwatch.Elapsed;

			float    percentageRendered = (float)renderJob.RawPixelsRendered / totalRawPix;
			ulong    rawPixelsRemaining = totalRawPix - renderJob.RawPixelsRendered;
			int      passesRemaining    = totalPasses - renderJob.PassesRendered;
			TimeSpan estimatedTotalTime = elapsed / percentageRendered;

			//TODO: Progress bars..
			const string timeFormat    = "h\\:mm\\:ss"; //Format string for timespan
			const string percentFormat = "p1";          //Format string for percentages
			const string numFormat     = "n0";
			const int    numAlign      = 15;
			const int    percentAlign  = 8;

			renderStatsTable.Rows.Clear();
			renderStatsTable.AddRow($"[{StatsCategoryMarkup}]Time[/]",         $"{elapsed.ToString(timeFormat)} elapsed");
			renderStatsTable.AddRow("",                                        $"{(estimatedTotalTime - elapsed).ToString(timeFormat)} remaining");
			renderStatsTable.AddRow("",                                        $"{estimatedTotalTime.ToString(timeFormat)} total");
			renderStatsTable.AddRow("",                                        "");
			renderStatsTable.AddRow($"[{StatsCategoryMarkup}]Pixels (Raw)[/]", $"{FormatU(renderJob.RawPixelsRendered, totalRawPix)} rendered");
			renderStatsTable.AddRow("",                                        $"{FormatU(rawPixelsRemaining,          totalRawPix)} remaining");
			renderStatsTable.AddRow("",                                        $"{totalRawPix.ToString(numFormat),numAlign}          total");
			renderStatsTable.AddRow("",                                        "");
			renderStatsTable.AddRow($"[{StatsCategoryMarkup}]Image [/]",       $"{totalTruePixels.ToString(numFormat),numAlign}          pixels total");
			renderStatsTable.AddRow("",                                        $"{renderJob.ImageBuffer.Width.ToString(numFormat),numAlign}          pixels wide");
			renderStatsTable.AddRow("",                                        $"{renderJob.ImageBuffer.Height.ToString(numFormat),numAlign}          pixels high");
			renderStatsTable.AddRow("",                                        "");
			renderStatsTable.AddRow($"[{StatsCategoryMarkup}]Passes[/]",       $"{FormatI(renderJob.PassesRendered, totalPasses)} rendered");
			renderStatsTable.AddRow("",                                        $"{FormatI(passesRemaining,          totalPasses)} remaining");
			renderStatsTable.AddRow("",                                        $"{totalPasses.ToString(numFormat),numAlign}          total");
			renderStatsTable.AddRow("",                                        "");
			renderStatsTable.AddRow($"[{StatsCategoryMarkup}]Rays[/]",         $"{FormatU(renderJob.RaysScattered,       rayCount)} scattered");
			renderStatsTable.AddRow("",                                        $"{FormatU(renderJob.RaysAbsorbed,        rayCount)} absorbed");
			renderStatsTable.AddRow("",                                        $"{FormatU(renderJob.BounceLimitExceeded, rayCount)} exceeded");
			renderStatsTable.AddRow("",                                        $"{FormatU(renderJob.SkyRays,             rayCount)} sky");
			renderStatsTable.AddRow("",                                        $"{rayCount.ToString(numFormat),numAlign}          total");
			renderStatsTable.AddRow("",                                        "");
			renderStatsTable.AddRow($"[{StatsCategoryMarkup}]Scene[/]",        $"[{SceneMarkup}]{renderJob.Scene}[/]");
			renderStatsTable.AddRow("",                                        $"{renderJob.Scene.Camera}");
			renderStatsTable.AddRow("",                                        $"{renderJob.Scene.SkyBox}");
			renderStatsTable.AddRow("",                                        "");
			renderStatsTable.AddRow($"[{StatsCategoryMarkup}]Depth Buffer[/]", "[bold italic slowblink red]Coming soon...[/]");
			renderStatsTable.AddRow("",                                        "");
			renderStatsTable.AddRow($"[{StatsCategoryMarkup}]Console[/]",      $"CWin: ({Console.WindowWidth}x{Console.WindowHeight})");
			renderStatsTable.AddRow("",                                        $"CBuf: ({Console.BufferWidth}x{Console.BufferHeight})");
			renderStatsTable.AddRow("",                                        $"Ansi: ({AnsiConsole.Console.Profile.Width}x{AnsiConsole.Console.Profile.Height})");


			static string FormatU(ulong val, ulong total)
			{
				return $"{val.ToString(numFormat),numAlign} {'(' + ((float)val / total).ToString(percentFormat) + ')',percentAlign}";
			}

			static string FormatI(int val, int total)
			{
				return $"{val.ToString(numFormat),numAlign} {'(' + ((float)val / total).ToString(percentFormat) + ')',percentAlign}";
			}

		#endregion

		#region Putting it all together

			statsAndImageTable.AddRow(renderStatsTable /* */, imageRenderable /**/);

		#endregion
		}
	}

	/// <summary>
	///  Function to be called once a render job is finished. Also returns the rendered image
	/// </summary>
	private static Image<Rgb24> FinalizeRenderJob(AsyncRenderJob renderJob)
	{
		Image<Rgb24> image = renderJob.GetAwaiter().GetResult();
		AnsiConsole.MarkupLine($"[{FinishedRenderMarkup}]Finished Rendering in {renderJob.Stopwatch.Elapsed:h\\:mm\\:ss}[/]");

		//Print any errors
		if (!GraphicsValidator.Errors.IsEmpty)
		{
			ConcurrentDictionary<GraphicsErrorType, ConcurrentDictionary<object, ulong>> errors = GraphicsValidator.Errors;
			//Print a list of all the errors that occurred
			Table table = new()
			{
					Title = new TableTitle($"[{TitleMarkup}]Errors occured during render:[/]")
			};
			//I chose to have the error type on the top (column) and the object on the left (row)
			//We have to build a comprehensive list of all possible rows and columns that occur in all dimensions/levels of the dictionaries (not all objects will exist for all error types and vice versa)
			GraphicsErrorType[] allErrorTypes = Enum.GetValues<GraphicsErrorType>(); //All possible enum values for the types of error that can occur
			HashSet<object>     allObjects    = new();                               //Aggregated of all the objects that had any errors (can be just one type of error or all types)

			//Important to create all the columns first, before we create the rows, or we get exceptions (not enough columns)
			table.AddColumn($"[{HeadingMarkup}]Erroring object[/]");
			foreach (GraphicsErrorType errorType in allErrorTypes) table.AddColumn($"[{HeadingMarkup}]{errorType}[/]");
			//Aggregate all the objects that had errors
			foreach (GraphicsErrorType errorType in allErrorTypes)
			{
				if (!errors.TryGetValue(errorType, out ConcurrentDictionary<object, ulong>? objectMap)) continue;
				foreach (object obj in objectMap.Keys) allObjects.Add(obj);
			}

			//Build the rows. Each row represents the erroring object, and the error counts for it
			var    row            = new IRenderable[allErrorTypes.Length + 1];
			Markup noErrorsMarkup = new("[dim italic green]N/A[/]"); //The error never occurred for this object
			foreach (object obj in allObjects)
			{
				//Fill the array with error messages so that I can tell when i mess up, also saves the app from crashing if that happens (because the default is null)
				Array.Fill(row, new Markup("[bold red underline rapidblink]INTERNAL ERROR[/]"));
				row[0] = new Markup(obj.ToString()!); //First item is the object's name
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
			//TODO: Separate style for this
			AnsiConsole.MarkupLine($"[{FinishedRenderMarkup}]No errors occured during render[/]");
		}

		return image;
	}

	/// <inheritdoc/>
	public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
	{
		//Get the settings for how and what we'll render
		(Scene scene, RenderOptions renderOptions) = ConfirmSettings(context, settings);

		//Start the render job and display the progress while we wait
		AsyncRenderJob renderJob = new(scene, renderOptions);
		await DisplayProgress(renderJob);

		//Finalize everything
		Image<Rgb24> image = FinalizeRenderJob(renderJob);

		//Save and open the image for viewing
		await image.SaveAsync(File.OpenWrite(settings.OutputFile), new PngEncoder());
		await Process.Start(
				new ProcessStartInfo
				{
						FileName  = "eog",
						Arguments = $"\"{settings.OutputFile}\"",
						//These flags stop the image display program's console from attaching to ours (because that's yuck!)
						UseShellExecute        = false,
						RedirectStandardError  = true,
						RedirectStandardInput  = true,
						RedirectStandardOutput = true
				}
		)!.WaitForExitAsync();
		return 0;
	}

	[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
	internal sealed class Settings : CommandSettings
	{
		//TODO: Tie into RenderOptions.Default
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