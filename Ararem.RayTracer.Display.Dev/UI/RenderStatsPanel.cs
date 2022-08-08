using Ararem.RayTracer.Core;
using Eto.Forms;
using LibArarem.Core.Logging;
using Serilog;
using System;
using System.Diagnostics;

namespace Ararem.RayTracer.Display.Dev.UI;

public sealed partial class RenderJobPanel
{
	/// <summary><see cref="Panel"/> class that displays the statistics of a <see cref="RenderJob"/></summary>
	private sealed class RenderStatsPanel : Panel
	{
		private readonly ILogger log;

		public RenderStatsPanel(RenderJobPanel parentJobPanel)
		{
			ParentJobPanel = parentJobPanel;
			log            = LogUtils.WithInstanceContext(this);
		}

		/// <summary>The <see cref="RenderJobPanel"/> that contains this instance as a child object (aka the panel that created this panel)</summary>
		public RenderJobPanel ParentJobPanel { get; }

		/// <summary>Main <see cref="DynamicLayout"/> for this panel. Is the same as accessing <see cref="Panel.Content"/></summary>
		public DynamicLayout Layout { get; private set; }

		/// <inheritdoc/>
		protected override void OnPreLoad(EventArgs e)
		{
			log.TrackEvent(this, e);
			Content = Layout = new DynamicLayout
			{
					ID      = $"{ID}/Content",
					Spacing = DefaultSpacing,
					Padding = DefaultPadding
			};
			base.OnPreLoad(e);
		}

		public void Update()
		{
			Stopwatch sw = Stopwatch.StartNew();

			Invalidate(true); //Mark for redraw
			log.Verbose("Stats updated in {Elapsed:#00.000 'ms'}", sw.Elapsed.TotalMilliseconds);
		}
	}
}