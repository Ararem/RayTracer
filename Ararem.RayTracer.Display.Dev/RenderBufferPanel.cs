using Eto.Containers;
using Eto.Drawing;
using Eto.Forms;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Diagnostics;

namespace Ararem.RayTracer.Display.Dev;

public sealed partial class RenderJobPanel
{
	private sealed class RenderBufferPanel : Panel
	{
		public RenderBufferPanel(RenderJobPanel parentJobPanel)
		{
			ParentJobPanel = parentJobPanel;
			ID             = $"{ParentJobPanel.ID}/Buffer";
			Content = ImageView = new DragZoomImageView
			{
					ID = $"{ID}/ImageView", ZoomButton = MouseButtons.Middle
			};
			ImageView.Image = PreviewImage = new Bitmap(2,2, PixelFormat.Format24bppRgb, new[]{Colors.Black, Colors.Magenta, Colors.Black, Colors.Magenta})
			{
					ID = $"{ImageView.ID}/TEMP_WILL_BE_REPLACED_ONCE_UI_UPDATED",
			}; //Assign something so that it's not null
		}

		/// <summary>The <see cref="RenderJobPanel"/> that contains this instance as a child object (aka the panel that created this panel)</summary>
		public RenderJobPanel ParentJobPanel { get; }
		/// <summary>
		/// The <see cref="DragZoomImageView"/> that is used to display the render buffer (also the content of this panel)
		/// </summary>
		public DragZoomImageView ImageView    { get; }

		/// <summary>
		/// The bitmap image that is displayed by <see cref="ImageView"/>
		/// </summary>
		public Bitmap            PreviewImage { get; private set; }

		public void Update()
		{
			Stopwatch        sw              = Stopwatch.StartNew();
			Buffer2D<Rgb24>? srcRenderBuffer = ParentJobPanel.RenderJob?.Image.Frames.RootFrame.PixelBuffer;
			if (srcRenderBuffer is not null)
			{
				int width = srcRenderBuffer.Width, height = srcRenderBuffer.Height;
				if ((PreviewImage.Width != width) || (PreviewImage.Height != height))
				{
					Debug("Recreating preview image ({OldValue}) to change size to {NewValue}", PreviewImage.Size, new Size(width, height));
					ImageView.Image = null;
					PreviewImage.Dispose();
					ImageView.Image = PreviewImage = new Bitmap(width, height, PixelFormat.Format24bppRgb) { ID = $"{ImageView.ID}.Bitmap" };
				}

				using BitmapData destPreviewImage = PreviewImage.Lock();
				IntPtr           destOffset       = destPreviewImage.Data;
				int              xSize            = PreviewImage.Width, ySize = PreviewImage.Height;
				for (int y = 0; y < ySize; y++)
				{
					//This code assumes the source and dest images are same bit depth and size
					//Otherwise here be dragons
					unsafe
					{
						Span<Rgb24> renderBufRow = srcRenderBuffer.DangerousGetRowSpan(y);
						void*       destPtr      = destOffset.ToPointer();
						Span<Rgb24> destRow      = new(destPtr, xSize);

						renderBufRow.CopyTo(destRow);
						destOffset += destPreviewImage.ScanWidth;
					}
				}
			}

			Invalidate(true);                                           //Mark for redraw
			((DocumentPage)ParentJobPanel.Parent).Image = PreviewImage; //Make the icon of the DocumentPage be the same as the buffer
			ForContext("Control", this).Verbose("Image updated in {Elapsed:#00.000 'ms'}", sw.Elapsed.TotalMilliseconds);
		}
	}
}