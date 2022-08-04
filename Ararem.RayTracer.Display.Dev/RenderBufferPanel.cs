using Ararem.RayTracer.Core;
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
		/// <summary>The <see cref="RenderJobPanel"/> that contains this instance as a child object (aka the panel that created this panel)</summary>
		public RenderJobPanel ParentJobPanel { get; }

		/// <summary>Main <see cref="DynamicLayout"/> for this panel. Is the same as accessing <see cref="Panel.Content"/></summary>
		public DynamicLayout Layout { get; }
	#region Ease-of-use properties (shortcut properties)

		private Scene SelectedScene
		{
			get => ParentJobPanel.SelectedScene;
			set => ParentJobPanel.SelectedScene = value;
		}

		private RenderOptions RenderOptions => ParentJobPanel.RenderOptions;

		private RenderJob? RenderJob
		{
			get => ParentJobPanel.RenderJob;
			set => ParentJobPanel.RenderJob = value;
		}

	#endregion

		public DragZoomImageView ImageView    { get; }
		public Bitmap            PreviewImage { get; private set; }

		public RenderBufferPanel(RenderJobPanel parentJobPanel)
		{
			ParentJobPanel = parentJobPanel;
			Content = Layout = new DynamicLayout
			{
					ID      = $"{ID}/Content",
					Spacing = DefaultSpacing,
					Padding = DefaultPadding
			};
			DynamicGroup group = Layout.BeginGroup("Render Buffer", spacing: DefaultSpacing, padding: DefaultPadding);
			//TODO: ID's
			PreviewImage      = new Bitmap(new Size(1, 1), PixelFormat.Format24bppRgb); //Assign something so that it's not null
			ImageView  = new DragZoomImageView { ID = "Preview Image View", ZoomButton = MouseButtons.Middle };
			Layout.Add(ImageView);
			Layout.EndGroup();

			Layout.Create();
			group.GroupBox.Style = nameof(Force_Heading);
		}

		public void Update()
		{
			Stopwatch        sw              = Stopwatch.StartNew();
			Buffer2D<Rgb24>? srcRenderBuffer = RenderJob?.Image.Frames.RootFrame.PixelBuffer;
			if (srcRenderBuffer is not null)
			{
				int width = srcRenderBuffer.Width, height = srcRenderBuffer.Height;
				if ((PreviewImage.Width != width) || (PreviewImage.Height != height))
				{
					Debug("Recreating preview image ({OldValue}) to change size to {NewValue}", PreviewImage.Size, new Size(width, height));
					ImageView.Image = null;
					PreviewImage.Dispose();
					ImageView.Image                             = PreviewImage = new Bitmap(width, height, PixelFormat.Format24bppRgb) { ID = $"{ImageView.ID}.Bitmap" };
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
			Invalidate(true); //Mark for redraw
			((DocumentPage)ParentJobPanel.Parent).Image = PreviewImage; //Make the icon of the DocumentPage be the same as the buffer
			Verbose("[{Sender}] Image updated in {Elapsed:#00.000 'ms'}", this, sw.Elapsed.TotalMilliseconds);
		}
	}
}