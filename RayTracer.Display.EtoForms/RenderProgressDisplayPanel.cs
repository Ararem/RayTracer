using Eto.Drawing;
using Eto.Forms;
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
	private readonly TableLayout statsTable;

	public RenderProgressDisplayPanel(AsyncRenderJob renderJob)
	{
		//TODO
		this.renderJob = renderJob;
		Verbose("Creating StackPanelLayout for content");
		statsTable     = new TableLayout { ID                                                                                       = "Stats Table" };
		statsContainer = new GroupBox { Text                                                                                        = "Statistics", Content = statsTable, ID = "Stats Container" };
		previewImage   = new Bitmap(renderJob.RenderOptions.Width, renderJob.RenderOptions.Height, PixelFormat.Format24bppRgb) { ID = "Preview Image" };
		imageView      = new ImageView { Image                                                                                      = previewImage, ID   = "Image View" };
		imageContainer = new GroupBox { Text                                                                                        = "Preview", Content = imageView, ID = "Image Container" };
		Content = new StackLayout
		{
				Items       = { statsContainer, imageContainer },
				Orientation = Orientation.Horizontal,
				Spacing     = 10,
				ID = "Main Content StackLayout"
		};

		TaskWatcher.Watch(Task.Run(UpdatePreviewWorker), false);
	}

	private async Task UpdatePreviewWorker()
	{
		while (!renderJob.RenderCompleted)
		{
			await Application.Instance.InvokeAsync(Update);
			await Task.Delay(1000);
		}

		//Do final run to ensure image isn't half updated (if render completed partway through update)
		await Application.Instance.InvokeAsync(Update);

		void Update()
		{
			UpdateImagePreview();
			// UpdateStatsTable();

			Invalidate();
			Verbose("Invalidated for redraw");
		}
	}

	private void UpdateImagePreview()
	{
		MinimumSize                = new Size(160, 90);
		imageContainer.MinimumSize = new Size(160, 90);
		imageContainer.Size        = new Size(-1,  -1);
		imageContainer.ClientSize  = new Size(-1,  -1);
		imageView.Size             = new Size(-1,  -1);

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
	//
	// private void UpdateStatsTable()
	// {
	// 	const string timeFormat     = "h\\:mm\\:ss"; //Format string for Timespan
	// 	const string dateTimeFormat = "G";           //Format string for DateTime
	// 	const string percentFormat  = "p1";          //Format string for percentages
	// 	const string numFormat      = "n0";
	// 	const int    numAlign       = 15;
	// 	const int    percentAlign   = 8;
	//
	// 	Verbose("Updating stats table");
	// 	Stopwatch stop = Stopwatch.StartNew();
	//
	// 	int row = 0;
	//
	// 	statsContainer.Content = "?????? THIS DONT WORK";
	// 	statsTable.Rows.Clear();
	// 	statsTable.Rows.Add(new TableRow("Test", "Test 2"));
	// 	statsTable.Update();
	//
	// 	Verbose("Finished updating stats in {Elapsed}", stop.Elapsed);
	//
	// 	static string FormatU(ulong val, ulong total)
	// 	{
	// 		return $"{val.ToString(numFormat),numAlign} {'(' + ((float)val / total).ToString(percentFormat) + ')',percentAlign}";
	// 	}
	//
	// 	static string FormatI(int val, int total)
	// 	{
	// 		return $"{val.ToString(numFormat),numAlign} {'(' + ((float)val / total).ToString(percentFormat) + ')',percentAlign}";
	// 	}
	// }
}