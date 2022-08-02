using Ararem.RayTracer.Core;
using Ararem.RayTracer.Impl.Builtin;
using Eto.Forms;
using System;
using System.Diagnostics;
using System.Threading;

namespace Ararem.RayTracer.Display.Dev;

/// <summary>
/// A <see cref="Panel"/> that exposes a GUI for an <see cref="Core.RenderJob"/>, 
/// </summary>
public partial class RenderJobPanel : Panel
{
	private readonly RenderBufferPanel renderBufferPanel;

	private readonly RenderControllerPanel renderControllerPanel;
	private readonly RenderStatsPanel   renderStatsPanel;


	//TODO: Save image button
	public RenderJobPanel(string id)
	{
		ID      = id;
		Padding = DefaultPadding;
		{
			Content = Layout = new DynamicLayout
			{
					ID      = $"{ID}/Content",
					Spacing = DefaultSpacing,
					Padding = DefaultPadding
			};
		}
		//Split the layout into 3 horizontal groups - options, stats, image
		Layout.BeginHorizontal();

		Layout.Add(renderControllerPanel = new RenderControllerPanel(this));
		Layout.Add(renderStatsPanel   = new RenderStatsPanel(this));
		Layout.Add(renderBufferPanel  = new RenderBufferPanel(this));

		Layout.EndHorizontal();
		Layout.Create();

		{
			// Periodically update the previews using a timer
			//PERF: This creates quite a few allocations when called frequently
			//TODO: Perhaps PeriodicTimer or UITimer
			#warning Need to dispose timer when panel closed
			#warning Need to cancel render when panel closed
			//TODO: Button to save image
			updatePreviewTimer = new Timer(static state => Application.Instance.Invoke((Action)state!), UpdateUi, UpdatePeriod, Timeout.Infinite);
		}
	}

	/// <summary>Main <see cref="DynamicLayout"/> for this panel. Is the same as accessing <see cref="Panel.Content"/></summary>
	public DynamicLayout Layout { get; }

	/// <summary>The current render job (if any)</summary>
	public RenderJob? RenderJob { get; private set; } = null;

	/// <summary>Currently selected scene. If a render is running, then it's the one that's being rendered.</summary>
	public Scene SelectedScene { get; private set; } = BuiltinScenes.Demo;

	/// <summary>Render options that affect how the <see cref="RenderJob"/> is rendered</summary>
	public RenderOptions RenderOptions { get; } = new();

#region UI Controls

	/// <summary>Target refreshes-per-second that we want</summary>
	private static int TargetRefreshRate => 1; //Fps

	/// <summary>Time (ms) between updates for our <see cref="TargetRefreshRate"/></summary>
	private static int UpdatePeriod => 1000 / TargetRefreshRate;

	/// <summary>Updates all the UI</summary>
	private void UpdateUi()
	{
		/*
		 * Note that we don't have to worry about locks or anything, since
		 * (A) - It's only called on the main thread
		 * (B) - The timer is only ever reset *after* everything's already been updated
		 */
		Stopwatch totalSw = Stopwatch.StartNew();
		Verbose("[{Sender}] Updating UI", ID);

		renderControllerPanel.Update();
		renderStatsPanel.Update();
		renderBufferPanel.Update();

		Verbose("[{Sender}] Updated in {Elapsed:#00.000 'ms'}", ID, totalSw.Elapsed.TotalMilliseconds);

		if (!updatePreviewTimer.Change(UpdatePeriod, Timeout.Infinite))
		{
			Error("Could not set preview timer to call again (expect static UI)");
		}
	}

	private readonly Timer updatePreviewTimer;

#endregion
}