using Ararem.RayTracer.Core;
using Eto.Forms;

namespace Ararem.RayTracer.Display.Dev;

public partial class RenderJobPanel
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

		public void Update()
		{

		}
	}
}