using Eto.Containers;
using Eto.Drawing;
using Eto.Forms;
using LibEternal.ObjectPools;
using RayTracer.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using static Serilog.Log;
using Size = Eto.Drawing.Size;

// using NetFabric.Hyperlinq;

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
	private readonly Pen       depthBufferPen = new(Colors.Gray);

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

	private readonly Timer updatePreviewTimer;

	/// <summary>
	///  Render stats from the last time we updated the preview
	/// </summary>
	private RenderStats prevUpdateStats;

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

		//Periodically update the previews using a timer
		//PERF: This creates quite a few allocations when called frequently
		updatePreviewTimer = new Timer(static state => Application.Instance.Invoke((Action)state!), UpdateAllPreviews, 0, 1000/60);
	}

	/// <summary>
	///  Updates all the previews. Important that it isn't called directly, but by <see cref="Application.Invoke{T}"/> so that it's called on the main thread
	/// </summary>
	private void UpdateAllPreviews()
	{
		UpdateImagePreview();
		UpdateStatsTable(renderJob.RenderStats);

		Invalidate();
	}

	private void UpdateImagePreview()
	{
		using BitmapData data         = previewImage.Lock();
		int              xSize        = previewImage.Width, ySize = previewImage.Height;
		Image<Rgb24>     renderBuffer = renderJob.ImageBuffer;
		IntPtr           offset       = data.Data;
		for (int y = 0; y < ySize; y++)
			//This code assumes the source and dest images are same bit depth and size
			//Otherwise here be dragons
			unsafe
			{
				Span<Rgb24> renderBufRow = renderBuffer.GetPixelRowSpan(y);
				void*       destPtr      = offset.ToPointer();
				Span<Rgb24> destRow      = new(destPtr, xSize);

				renderBufRow.CopyTo(destRow);
				offset += data.ScanWidth;
			}
	}

	private void UpdateStatsTable(RenderStats renderStats)
	{
		List<(string Title, (string Name, string Value)[] NamedValues)> stringStats = new();

		{
			TimeSpan elapsed = renderJob.Stopwatch.Elapsed;
			TimeSpan estimatedTotalTime;
			//If the percentage rendered is very low, the division results in a number that's too large to fit in a timespan, which throws
			try
			{
				estimatedTotalTime = elapsed / ((float)renderStats.RawPixelsRendered / renderStats.TotalRawPixels);
			}
			catch (OverflowException)
			{
				estimatedTotalTime = TimeSpan.FromDays(69.420); //If something's broke at least let me have some fun
			}

			stringStats.Add(
					("Time", new[]
					{
							("Elapsed", FormatTime(elapsed)),
							("Remaining", FormatTime(estimatedTotalTime - elapsed)),
							("Total", FormatTime(estimatedTotalTime)),
							("ETC", FormatDate(DateTime.Now + (estimatedTotalTime - elapsed)) + "\t") //We need to add a tab here or else the width keeps changing (thanks non-monospaced fonts!)
					})
			);
		}
		{
			stringStats.Add(("Raw Pixels", new[]
			{
					("Rendered",FormatUlongRatio(renderStats.RawPixelsRendered,  renderStats.TotalRawPixels)),
					("Remaining",FormatUlongRatio(renderStats.TotalRawPixels-renderStats.RawPixelsRendered, renderStats.TotalRawPixels)),
					("Total",FormatUlong(renderStats.TotalRawPixels)),
			}));
		}
		{
			stringStats.Add(("Image", new[]
			{
					//Assumes preview image has same dimensions as render buffer (which should always be the case)
					("Width",FormatInt(previewImage.Width)),
					("Height",FormatInt(previewImage.Height)),
					("Pixels",FormatInt(renderStats.TotalTruePixels)),
			}));
		}
		{
			stringStats.Add(("Passes", new[]
			{
					("Rendered",FormatIntRatio(renderStats.PassesRendered, renderJob.RenderOptions.Passes)),
					("Remaining",FormatIntRatio(renderStats.PassesRendered, renderJob.RenderOptions.Passes)),
					("Total",FormatInt(renderJob.RenderOptions.Passes)),
					("Progress", FormatUlongRatio(renderStats.RawPixelsRendered % (ulong)renderStats.TotalTruePixels, (ulong)renderStats.TotalTruePixels))
			}));
		}
		{
			stringStats.Add(("Rays", new[]
			{
					("Scattered",FormatUlongRatio(renderStats.RaysScattered, renderStats.RayCount)),
					("Absorbed",FormatUlongRatio(renderStats.RaysAbsorbed, renderStats.RayCount)),
					("Exceeded",FormatUlongRatio(renderStats.BounceLimitExceeded, renderStats.RayCount)),
					("Sky",FormatUlongRatio(renderStats.SkyRays, renderStats.RayCount)),
					("Total",FormatUlong(renderStats.RayCount))
			}));
		}
		{
			stringStats.Add(("Scene", new[]
			{
					("Name",renderJob.Scene.Name),
					("Object Count",FormatInt(renderJob.Scene.SceneObjects.Length)),
					("Light Count",FormatInt(renderJob.Scene.Lights.Length)),
					("Camera",renderJob.Scene.Camera.ToString()!),
			}));
		}
		{
			stringStats.Add(("Renderer", new[]
			{
					("Threads",FormatInt(renderStats.ThreadsRunning)),
					("Completed",renderJob.RenderCompleted.ToString()),
					("Task",renderJob.RenderTask.ToString()!),
			}));
		}

		//Due to how the table is implemented, I can't resize it later
		//So if the size doesn't match our array, we need to recreate it
		Size correctDims = new(3, stringStats.Count + 2); //+2 = 1 for depth + 1 for spacer row
		if (statsTable.Dimensions != correctDims)
		{
			Verbose("Old table dims {Dims} do not match stats array, disposing and recreating with dims {NewDims}", statsTable.Dimensions, correctDims);
			statsTable.Detach();
			statsTable.Dispose();
			statsTable             = new TableLayout(correctDims) { ID = "Stats Table", Spacing = new Size(10, 10), Padding = 10, Size = new Size(0, 0) };
			statsContainer.Content = statsTable;
		}

		for (int rowIdx = 0; rowIdx < stringStats.Count; rowIdx++)
		{
			TableRow row = statsTable.Rows[rowIdx];
			(string title, (string Name, string Value)[] namedValues) = stringStats[rowIdx];
			//Aggregate the name and value texts
			string aggregatedNames  = StringBuilderPool.BorrowInline(static (sb, namedValues) => { sb.AppendJoin(Environment.NewLine, namedValues.Select(val => $"{val.Name}:")); }, namedValues);
			string aggregatedValues = StringBuilderPool.BorrowInline(static (sb, namedValues) => { sb.AppendJoin(Environment.NewLine, namedValues.Select(val => val.Value)); },      namedValues);

			//Get the Labels at the correct locations, or assign them if needed
			if (row.Cells[0].Control is not Label titleLabel)
			{
				Verbose("Cell [{Position}] was not label (was {Control}), disposing and updating", (0, rowIdx), row.Cells[0].Control);
				row.Cells[0]?.Control?.Detach();
				row.Cells[0]?.Control?.Dispose(); //Dispose the old control
				statsTable.Add(titleLabel = new Label(), 0, rowIdx);
			}

			if (row.Cells[1].Control is not Label nameLabel)
			{
				Verbose("Cell [{Position}] was not name label (was {Control}), disposing and updating", (1, rowIdx), row.Cells[1].Control);
				row.Cells[1]?.Control?.Detach();
				row.Cells[1]?.Control?.Dispose(); //Dispose of the old control
				statsTable.Add(nameLabel = new Label(), 1, rowIdx);
			}

			if (row.Cells[2].Control is not Label valueLabel)
			{
				Verbose("Cell [{Position}] was not value label (was {Control}), disposing and updating", (2, rowIdx), row.Cells[2].Control);
				row.Cells[2]?.Control?.Detach();
				row.Cells[2]?.Control?.Dispose(); //Dispose of the old control
				statsTable.Add(valueLabel = new Label(), 2, rowIdx);
			}


			titleLabel.Text = title;
			nameLabel.Text  = aggregatedNames;
			valueLabel.Text = aggregatedValues;
		}

		{
			int       row             = statsTable.Dimensions.Height - 2; //Account for start at 0 offset and last spacer row
			int       maxDepth        = renderJob.RenderOptions.MaxDepth;
			TableCell titleCell       = statsTable.Rows[row].Cells[0];
			TableCell descriptionCell = statsTable.Rows[row].Cells[1];
			TableCell depthBufferCell = statsTable.Rows[row].Cells[2];

			//Update title control type if needed
			if (titleCell.Control is not Label titleLabel)
			{
				Verbose("Depth Buffer Title Cell {Position} was not label (was {Control}), disposing and updating", (0, row), titleCell.Control);
				titleCell.Control?.Detach();
				titleCell.Control?.Dispose(); //Dispose of the old control
				statsTable.Add(titleLabel = new Label(), 0, row);
			}

			titleLabel.Text = "Depth Buffer"; //Update title control text

			//Update description control if needed
			if (descriptionCell.Control is not Label descLabel)
			{
				Verbose("Depth Buffer Description Cell {Position} was not Label (was {Control}), disposing and updating", (1, row), depthBufferCell.Control);
				depthBufferCell.Control?.Detach();
				depthBufferCell.Control?.Dispose(); //Dispose of the old control
				statsTable.Add(descLabel = new Label(), 1, row);
			}

			descLabel.Text = $"{depthBufferBitmap.Width}x{depthBufferBitmap.Height}";
			//Update image control if needed
			if (depthBufferCell.Control != depthBufferImageView)
			{
				Verbose("Depth Buffer Image Cell {Position} was not our ImageView (was {Control}), disposing and updating", (2, row), depthBufferCell.Control);
				depthBufferCell.Control?.Detach();
				depthBufferCell.Control?.Dispose(); //Dispose of the old control
				statsTable.Add(depthBufferImageView, 2, row);
			}

			depthBufferGraphics.Clear();

			//What I'm doing here is adjusting the depth values so that the largest one reaches the end of the graph (scaling up to fill the image)
			double[] doubleDepths = ArrayPool<double>.Shared.Rent(maxDepth);
			double   max          = 0;
			for (int depth = 0; depth < maxDepth; depth++) //Calculate the fractions and the max
			{
				#if true //Toggle whether to use a log function to compress the chart. Mostly needed when we have high max depth values
				const double b        = 800;
				double       m        = renderStats.RayCount;
				double       fraction = Math.Log((b * renderStats.RawRayDepthCounts[depth]) + 1, m) / Math.Log((b * m) + 1, m); //https://www.desmos.com/calculator/erite0if8u
				#else
				double fraction = renderStats.RawRayDepthCounts[depth] / renderStats.RawRayDepthCounts.Sum(u => (double)u); //https://www.desmos.com/calculator/erite0if8u
				#endif

				doubleDepths[depth] = fraction;
				max                 = Math.Max(max, fraction);
			}

			for (int depth = 0; depth < maxDepth; depth++)
			{
				double corrected = doubleDepths[depth] / max; //Adjust so the max == 1
				int    endX      = (int)Math.Min(corrected * DepthImageWidth, DepthImageWidth);
				depthBufferGraphics.DrawLine(depthBufferPen, 0, depth, endX, depth);
			}

			//Flush the image or it might not be drawn
			depthBufferGraphics.Flush();
		}

		prevUpdateStats = renderJob.RenderStats;

	#region Format Methods

		const string percentFormat = "p1"; //Format string for percentages
		const string numFormat     = "n0";
		const int    numAlign      = 15;
		const int    percentAlign  = 8;

		static string FormatTime(TimeSpan val)
		{
			return val.Days != 0 ? val.ToString("d'd 'hh':'mm':'ss'.'f").PadRight(numAlign) : val.ToString("hh':'mm':'ss'.'f").PadRight(numAlign);
		}

		static string FormatDate(DateTime val)
		{
			return val.ToString("G").PadRight(21);
		}

		static string FormatUlongRatio(ulong val, ulong total)
		{
			return $"{val.ToString(numFormat),numAlign} {'(' + ((float)val / total).ToString(percentFormat) + ')',percentAlign}";
		}
		//Format ULong
		static string FormatUlong(ulong val)
		{
			return $"{val.ToString(numFormat),numAlign}";
		}

		static string FormatIntRatio(int val, int total)
		{
			return $"{val.ToString(numFormat),numAlign} {'(' + ((float)val / total).ToString(percentFormat) + ')',percentAlign}";
		}
		static string FormatInt(int val)
		{
			return $"{val.ToString(numFormat),numAlign}";
		}

	#endregion
	}

	/// <inheritdoc/>
	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		updatePreviewTimer.Dispose();
	}
}