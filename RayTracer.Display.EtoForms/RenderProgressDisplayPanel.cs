using Eto.Containers;
using Eto.Drawing;
using Eto.Forms;
using LibEternal.ObjectPools;
using RayTracer.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using static Serilog.Log;
using Size = Eto.Drawing.Size;

namespace RayTracer.Display.EtoForms;

[SuppressMessage("ReSharper", "PrivateFieldCanBeConvertedToLocalVariable")]
internal sealed class RenderProgressDisplayPanel : Panel
{
	private const int DepthImageWidth = 100;

	/// <summary>
	///  Image used for the depth buffer
	/// </summary>
	private readonly Bitmap depthBufferBitmap;

	/// <summary>
	///  Graphics used for the depth buffer
	/// </summary>
	private readonly Graphics depthBufferGraphics;

	private readonly ImageView depthBufferImageView;

	/// <summary>
	///  The actual preview image buffer
	/// </summary>
	private readonly Bitmap previewImage;

	/// <summary>
	///  Container that draws a border and title around the preview image
	/// </summary>
	private readonly GroupBox previewImageContainer;

	/// <summary>
	///  The control that holds the preview image
	/// </summary>
	private readonly DragZoomImageView previewImageView;

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
		this.renderJob = renderJob;
		Verbose("Creating StackPanelLayout with content");
		statsTable = new TableLayout
				{ ID = "Stats Table", Size = new Size(0, 0) };
		statsContainer = new GroupBox
				{ ID = "Stats Container", Text = "Statistics", Content = statsTable, Size = new Size(0, 0), MinimumSize = new Size(1, 1) };

		previewImage = new Bitmap(renderJob.RenderOptions.Width, renderJob.RenderOptions.Height, PixelFormat.Format24bppRgb)
				{ ID = "Preview Bitmap" };
		previewImageView = new DragZoomImageView
				{ ID = "Preview Image View", Image = previewImage, Size = new Size(0, 0), ZoomButton = MouseButtons.Middle };
		previewImageContainer = new GroupBox
		{
				ID   = "Preview Image Container", Text = "Preview", Content      = previewImageView,
				Size = new Size(0, 0), MinimumSize     = new Size(1, 1), Padding = 10
		};

		depthBufferBitmap = new Bitmap(DepthImageWidth, renderJob.RenderOptions.MaxDepth, PixelFormat.Format32bppRgba)
				{ ID = "Depth Buffer Bitmap" };
		depthBufferImageView = new ImageView
				{ ID = "Image View", Image = depthBufferBitmap, Size = new Size(-1, -1) };
		depthBufferGraphics = new Graphics(depthBufferBitmap)
				{ ID = "Depth Buffer Graphics" };

		Content = new StackLayout
		{
				Items =
				{
						new StackLayoutItem(statsContainer,        VerticalAlignment.Stretch),
						new StackLayoutItem(previewImageContainer, VerticalAlignment.Stretch, true)
				},
				Orientation = Orientation.Horizontal,
				Spacing     = 10,
				Padding     = 10,
				ID          = "Main Content StackLayout"
		};

		TaskWatcher.Watch(Task.Run(UpdatePreviewWorker), false);
	}

	// ReSharper disable once FunctionNeverReturns
	private async Task UpdatePreviewWorker()
	{
		#if DEBUG //Never stop refreshing when in debug mode, so I can use Hot Reload to change things
		while (true)
				#else
		while (!renderJob.RenderCompleted)
				#endif
		{
			await Application.Instance.InvokeAsync(Update);
			await Task.Delay(10);
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

		(string Title, string[] Values)[] stringStats =
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
						$"Obj Count:	{renderJob.Scene.SceneObjects.Length}",
						$"Camera:		{renderJob.Scene.Camera}",
						$"SkyBox:		{renderJob.Scene.SkyBox}"
				}),
				("Renderer", new[]
				{
						$"{renderJob.ThreadsRunning} {(renderJob.ThreadsRunning == 1 ? "thread" : "threads")}"
				})
		};

		//Due to how the table is implemented, I can't rescale it later
		//So if the size doesn't match our array, we need to recreate it
		Size correctDims = new(2, stringStats.Length + 1);
		if (statsTable.Dimensions != correctDims)
		{
			Debug("Old table dims {Dims} do not match stats array, resizing to {NewDims}", statsTable.Dimensions, correctDims);
			statsTable             = new TableLayout(correctDims) { ID = "Stats Table", Spacing = new Size(10, 10), Padding = 10, Size = new Size(0, 0) };
			statsContainer.Content = statsTable;
		}

		Verbose("Updating stats strings");
		for (int i = 0; i < stringStats.Length; i++)
		{
			(string? title, string[]? strings) = stringStats[i];
			//WARN: Apparently too many `new`'s may be causing the errors i'm getting from GTK
			//Gotta reuse maybe
			string values = StringBuilderPool.BorrowInline(static (sb, vs) => sb.AppendJoin(Environment.NewLine, vs), strings);
			statsTable.Add(new Label { Text = title, ID  = $"{title} stats title" },  0, i);
			statsTable.Add(new Label { Text = values, ID = $"{title} stats values" }, 1, i);
		}

		{
			int maxDepth = renderJob.RenderOptions.MaxDepth;
			Verbose("Updating depth buffer graphics");
			statsTable.Add("Depth Buffer",       0, statsTable.Dimensions.Height - 1); //Last row
			statsTable.Add(depthBufferImageView, 1, statsTable.Dimensions.Height - 1);
			depthBufferGraphics.Clear();

			//What I'm doing here is adjusting the depth values so that the largest one reaches the end of the graph (scaling up to fill the image)
			double[] doubleDepths = ArrayPool<double>.Shared.Rent(maxDepth);
			double   max          = 0;
			for (int i = 0; i < maxDepth; i++) //Calculate the fractions and the max
			{
				#if true //Toggle whether to use a log function to compress the chart. Mostly needed when we have high max depth values
				const double b        = 0.000001;
				double       m        = rayCount;
				double       fraction = Math.Log((b * renderJob.RawRayDepthCounts[i]) + 1, m) / Math.Log((b * m) + 1, m); //https://www.desmos.com/calculator/erite0if8u
				#else
				double fraction = renderJob.RawRayDepthCounts[i] / System.Linq.Enumerable.Sum(renderJob.RawRayDepthCounts,u => (double)u); //https://www.desmos.com/calculator/erite0if8u
				#endif

				doubleDepths[i] = fraction;
				max             = Math.Max(max, fraction);
			}

			for (int i = 0; i < maxDepth; i++)
			{
				double corrected = doubleDepths[i] / max; //Adjust so the max == 1
				int    endX      = (int)Math.Min(corrected * DepthImageWidth, DepthImageWidth);
				depthBufferGraphics.DrawLine(Colors.Gray, 0, i, endX, i);
			}

			//Flush the image or it might not be drawn
			depthBufferGraphics.Flush();
		}

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