using Eto.Drawing;
using Eto.Forms;
using RayTracer.Core.Graphics;
using static Serilog.Log;

namespace RayTracer.Display.EtoForms;

internal sealed class RenderProgressDisplayPanel : Panel
{
	private readonly AsyncRenderJob renderJob;

	public RenderProgressDisplayPanel(AsyncRenderJob renderJob)
	{
		this.renderJob = renderJob;
		Verbose("Creating StackPanelLayout for content");
		//Layout is
		GroupBox statsGroupBox   = new(){Text = "Statistics"};
		GroupBox previewGroupBox = new(){Text = "Preview", Content = new ImageView(){Image = renderJob.ImageBuffer}};
		;
		Content = new StackLayout
		{
				Items   = { statsGroupBox, previewGroupBox },
				Orientation = Orientation.Horizontal,
				Spacing = 10
		};
	}
}