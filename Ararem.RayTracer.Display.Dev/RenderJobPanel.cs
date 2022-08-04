using Ararem.RayTracer.Core;
using Ararem.RayTracer.Impl.Builtin;
using Eto.Forms;
using System;
using System.Diagnostics;
using System.Threading;

namespace Ararem.RayTracer.Display.Dev;

/// <summary>
///  A <see cref="Panel"/> that exposes a GUI for an <see cref="Core.RenderJob"/>, with the ability to change the render options, selected scene, view
///  render statistics and view the render buffer
/// </summary>
public sealed partial class RenderJobPanel : Panel
{
	//TODO: These should directly show the controls, without a surrounding GroupBox. Extract the groupbox code to this main class
	/// <summary>Panel that displays the render buffer of the current <see cref="RenderJob"/></summary>
	private readonly RenderBufferPanel renderBufferPanel;

	/// <summary>Panel that controls the current <see cref="RenderJob"/> and it's render settings</summary>
	private readonly RenderControllerPanel renderControllerPanel;

	/// <summary>Panel that displays the statistics of the current <see cref="RenderJob"/></summary>
	private readonly RenderStatsPanel renderStatsPanel;

	/// <summary>Creates a new <see cref="RenderJobPanel"/></summary>
	/// <param name="id">string identifier for this instance</param>
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
		Layout.Add(renderControllerPanel = new RenderControllerPanel(this){ID = $"{ID}/Controller"});
		Layout.Add(renderStatsPanel      = new RenderStatsPanel(this){ID      = $"{ID}/Stats"});
		Layout.Add(renderBufferPanel     = new RenderBufferPanel(this){ID     = $"{ID}/Buffer"});

		Layout.EndHorizontal();
		Layout.Create();

		{
			// Periodically update the previews using a timer
			updateUiTimer = new UITimer(UpdateUi)
			{
					ID = $"{ID}/UITimer",Interval = 1f/TargetRefreshRate
			};
			updateUiTimer.Start();
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
	private static double TargetRefreshRate => 10; //Fps

	/// <summary>Updates all the UI</summary>
	private void UpdateUi(object? sender, EventArgs eventArgs)
	{
		//TODO: Add something to prevent UI freezes
		/*
		 * Note that we don't have to worry about locks or anything, since
		 * (A) - It's only called on the main thread, so that's thread safety done
		 * (B) - The timer is only ever reset *after* everything's already been updated, so no chance of two calls being executed at the same time
		 */
		Stopwatch totalSw = Stopwatch.StartNew();
		Verbose("[{Sender}] Updating UI", this);

		renderControllerPanel.Update();
		renderStatsPanel.Update();
		renderBufferPanel.Update();

		Verbose("[{Sender}] Updated in {Elapsed:#00.000 'ms'}", this, totalSw.Elapsed.TotalMilliseconds);
	}

	private readonly UITimer updateUiTimer;

#endregion

	/// <inheritdoc />
	protected override void Dispose(bool disposing)
	{
		Debug("[{Sender}]: Disposing", this);
		base.Dispose(disposing);
		//Dispose of all our child panels
		// renderBufferPanel?.Dispose();
		// renderControllerPanel?.Dispose();
		// renderStatsPanel?.Dispose();
		//Stop the UI timer from firing after we've disposed
		updateUiTimer?.Stop();
		updateUiTimer?.Dispose();
	}
}