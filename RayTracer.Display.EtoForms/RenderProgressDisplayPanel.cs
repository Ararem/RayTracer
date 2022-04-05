using Eto.Drawing;
using Eto.Forms;
using RayTracer.Core.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using static Serilog.Log;

namespace RayTracer.Display.EtoForms;

internal sealed class RenderProgressDisplayPanel : Panel
{
	private readonly TableLayout    statsTable;
	private readonly Bitmap         imageWidget;
	private readonly AsyncRenderJob renderJob;

	public RenderProgressDisplayPanel(AsyncRenderJob renderJob)
	{
		//TODO
		this.renderJob = renderJob;
		Verbose("Creating StackPanelLayout for content");
		statsTable = new TableLayout { };
		GroupBox statsGroupBox = new() { Text = "Statistics", Content = statsTable};
		imageWidget = new Bitmap(renderJob.RenderOptions.Width, renderJob.RenderOptions.Height, PixelFormat.Format24bppRgb);
		GroupBox previewGroupBox = new() { Text = "Preview", Content = new ImageView { Image = imageWidget } };
		Content = new StackLayout
		{
				Items       = { statsGroupBox, previewGroupBox },
				Orientation = Orientation.Horizontal,
				Spacing     = 10
		};

		Task.Run(UpdatePreviewWorker);
	}

	private async Task UpdatePreviewWorker()
	{
		while (!renderJob.RenderCompleted)
		{
			UpdateImagePreview();
			UpdateStatsTable();
			await Task.Delay(1000);
		}

		UpdateImagePreview(); //Do final run to ensure image isn't half updated (if render completed partway through update)
		UpdateStatsTable();
	}

	private void UpdateImagePreview()
	{
		using BitmapData data         = imageWidget.Lock();
		Stopwatch        stop         = Stopwatch.StartNew();
		int              xSize        = imageWidget.Width, ySize = imageWidget.Height;
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

		//Mark this object as requiring a redraw
		Invalidate();

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