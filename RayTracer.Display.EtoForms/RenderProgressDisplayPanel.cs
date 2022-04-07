using Eto.Drawing;
using Eto.Forms;
using LibEternal.ObjectPools;
using RayTracer.Core.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using static Serilog.Log;
using Size = Eto.Drawing.Size;

namespace RayTracer.Display.EtoForms;

internal sealed class RenderProgressDisplayPanel : Panel
{
	/// <summary>
	///  Container that draws a border and title around the preview image
	/// </summary>
	private readonly GroupBox imageContainer;

	/// <summary>
	///  The control that holds the preview image
	/// </summary>
	private readonly ImageView imageView;

	/// <summary>
	///  The actual preview image buffer
	/// </summary>
	private readonly Bitmap previewImage;

	/// <summary>
	///  Render job we are displaying the progress for
	/// </summary>
	private readonly AsyncRenderJob renderJob;

	/// <summary>
	///  Container that has a title and border around the stats table
	/// </summary>
	private readonly GroupBox statsContainer;

	/// <summary>
	///  Table that contains the various stats
	/// </summary>
	private TableLayout statsTable;

	public RenderProgressDisplayPanel(AsyncRenderJob renderJob)
	{
		//TODO
		this.renderJob = renderJob;
		Verbose("Creating StackPanelLayout for content");
		statsTable = new TableLayout
				{ ID = "Stats Table" };
		statsContainer = new GroupBox
				{ ID = "Stats Container", Text = "Statistics", Content = statsTable, MinimumSize = new Size(0,0) };
		previewImage = new Bitmap(renderJob.RenderOptions.Width, renderJob.RenderOptions.Height, PixelFormat.Format24bppRgb)
				{ ID = "Preview Image" };
		imageView = new ImageView
				{ ID = "Image View", Image = previewImage, Size = new Size(0,0) };
		imageContainer = new GroupBox
				{ Text = "Preview", Content = imageView, ID = "Image Container", MinimumSize = new Size(0,0) };
		Content = new StackLayout
		{
				Items =
				{
						new StackLayoutItem(statsContainer, VerticalAlignment.Stretch, false),
						new StackLayoutItem(imageContainer, VerticalAlignment.Stretch,       true)
				},
				Orientation = Orientation.Horizontal,
				Spacing     = 10,
				Padding = 10,
				ID          = "Main Content StackLayout"
		};

		TaskWatcher.Watch(Task.Run(UpdatePreviewWorker), false);
	}

	private async Task UpdatePreviewWorker()
	{
		#if DEBUG //Never stop refreshing when in debug mode, so I can use Hot Reload to change things
		while (true)
				#else
		while (!renderJob.RenderCompleted)
				#endif
		{
			await Application.Instance.InvokeAsync(Update);
			await Task.Delay(1000);
		}

		#if !DEBUG
		//Do final run to ensure image isn't half updated (if render completed partway through update)
		await Application.Instance.InvokeAsync(Update);
		#endif

		void Update()
		{
			UpdateImagePreview();
			UpdateStatsTable();

			Invalidate();
			Verbose("Invalidated for redraw");
		}
	}

	private void UpdateImagePreview()
	{
		using BitmapData data         = previewImage.Lock();
		Stopwatch        stop         = Stopwatch.StartNew();
		int              xSize        = previewImage.Width, ySize = previewImage.Height;
		Image<Rgb24>     renderBuffer = renderJob.ImageBuffer;
		Verbose("Updating image");
		IntPtr offset = data.Data;
		for (int y = 0; y < ySize; y++)
			unsafe
			{
				Span<Rgb24> renderBufRow = renderBuffer.GetPixelRowSpan(y);
				void*       destPtr      = offset.ToPointer();
				Span<Rgb24> destRow      = new(destPtr, xSize);

				renderBufRow.CopyTo(destRow);
				offset += data.ScanWidth;
			}

		Verbose("Finished updating image in {Elapsed}", stop.Elapsed);
	}

	private void UpdateStatsTable()
	{
		const string timeFormat     = "h\\:mm\\:ss"; //Format string for Timespan
		const string dateTimeFormat = "G";           //Format string for DateTime
		const string percentFormat  = "p1";          //Format string for percentages
		const string numFormat      = "n0";
		const int    numAlign       = 15;
		const int    percentAlign   = 8;

		int           totalTruePixels = renderJob.TotalTruePixels;
		ulong         totalRawPix     = renderJob.TotalRawPixels;
		ulong         rayCount        = renderJob.RayCount;
		RenderOptions options         = renderJob.RenderOptions;
		int           totalPasses     = options.Passes;
		TimeSpan      elapsed         = renderJob.Stopwatch.Elapsed;

		float    percentageRendered = (float)renderJob.RawPixelsRendered / totalRawPix;
		ulong    rawPixelsRemaining = totalRawPix - renderJob.RawPixelsRendered;
		int      passesRemaining    = totalPasses - renderJob.PassesRendered;
		TimeSpan estimatedTotalTime;
		//If the percentage rendered is very low, the division results in a number that's too large to fit in a timespan, which throws
		try
		{
			estimatedTotalTime = elapsed / percentageRendered;
		}
		catch (OverflowException)
		{
			estimatedTotalTime = TimeSpan.FromDays(69.420); //If something's broke at least let me have some fun
		}

		Verbose("Updating stats table");
		Stopwatch stop = Stopwatch.StartNew();

		(string Title, string[] Values)[] stats =
		{
				("Time", new[]
				{
						$"{elapsed.ToString(timeFormat),numAlign} elapsed",
						$"{(estimatedTotalTime - elapsed).ToString(timeFormat),numAlign} remaining",
						$"{estimatedTotalTime.ToString(timeFormat),numAlign} total",
						$"{(DateTime.Now + (estimatedTotalTime - elapsed)).ToString(dateTimeFormat),numAlign} ETC"
				}),
				("Pixels", new[]
				{
						$"{FormatU(renderJob.RawPixelsRendered, totalRawPix)} rendered",
						$"{FormatU(rawPixelsRemaining,          totalRawPix)} remaining",
						$"{totalRawPix.ToString(numFormat),numAlign}          total"
				}),
				("Image", new[]
				{
						$"{totalTruePixels.ToString(numFormat),numAlign}          pixels total",
						$"{options.Width.ToString(numFormat),numAlign}          pixels wide",
						$"{options.Height.ToString(numFormat),numAlign}          pixels high"
				}),
				("Passes", new[]
				{
						$"{FormatI(renderJob.PassesRendered, totalPasses)} rendered",
						$"{FormatI(passesRemaining,          totalPasses)} remaining",
						$"{totalPasses.ToString(numFormat),numAlign}          total"
				}),
				("Rays", new[]
				{
						$"{FormatU(renderJob.RaysScattered,       rayCount)} scattered",
						$"{FormatU(renderJob.RaysAbsorbed,        rayCount)} absorbed",
						$"{FormatU(renderJob.BounceLimitExceeded, rayCount)} exceeded",
						$"{FormatU(renderJob.SkyRays,             rayCount)} sky",
						$"{rayCount.ToString(numFormat),numAlign}          total"
				}),
				("Scene", new[]
				{
						$"Name:		{renderJob.Scene.Name}",
						$"Obj Count:	{renderJob.Scene.Objects.Length}",
						$"Camera:		{renderJob.Scene.Camera}",
						$"SkyBox:		{renderJob.Scene.SkyBox}"
				})
		};

		//Due to how the table is implemented, I can't rescale it later
		//So if the size doesn't match our array, we need to recreate it
		if (statsTable.Dimensions.Height != stats.Length) statsTable = new TableLayout(stats.Length, 2) { ID = "Stats Table" };

		for (int i = 0; i < stats.Length; i++)
		{
			(string? title, string[]? strings) = stats[i];
			string values = StringBuilderPool.BorrowInline(static (sb, vs) => sb.AppendJoin(Environment.NewLine, vs), strings);
			Debug("\n**{Category}:**\n{Values}", title, values);
			statsTable.Add(title,  i, 0);
			statsTable.Add(values, i, 1);
		}

		statsTable.Size = new Size(100, 500);

		Verbose("Finished updating stats in {Elapsed}", stop.Elapsed);

		static string FormatU(ulong val, ulong total)
		{
			return $"{val.ToString(numFormat),numAlign} {'(' + ((float)val / total).ToString(percentFormat) + ')',percentAlign}";
		}

		static string FormatI(int val, int total)
		{
			return $"{val.ToString(numFormat),numAlign} {'(' + ((float)val / total).ToString(percentFormat) + ')',percentAlign}";
		}
	}
}