using Ararem.RayTracer.Core;
using Eto.Forms;
using System.Diagnostics;

namespace Ararem.RayTracer.Display.Dev;

public sealed partial class RenderJobPanel
{
	/// <summary><see cref="Panel"/> class that displays the statistics of a <see cref="RenderJob"/></summary>
	private sealed class RenderStatsPanel : Panel
	{
		public RenderStatsPanel(RenderJobPanel parentJobPanel)
		{
			ParentJobPanel = parentJobPanel;
			Content = Layout = new DynamicLayout
			{
					ID      = $"{ID}/Content",
					Spacing = DefaultSpacing,
					Padding = DefaultPadding
			};
		}

		/// <summary>The <see cref="RenderJobPanel"/> that contains this instance as a child object (aka the panel that created this panel)</summary>
		public RenderJobPanel ParentJobPanel { get; }

		/// <summary>Main <see cref="DynamicLayout"/> for this panel. Is the same as accessing <see cref="Panel.Content"/></summary>
		public DynamicLayout Layout { get; }

		public void Update()
		{
			Stopwatch sw = Stopwatch.StartNew();

			Invalidate(true); //Mark for redraw
			ForContext("Control", this).Verbose("Stats updated in {Elapsed:#00.000 'ms'}", sw.Elapsed.TotalMilliseconds);
		}
	}
}