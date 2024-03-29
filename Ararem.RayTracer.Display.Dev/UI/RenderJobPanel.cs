using Ararem.RayTracer.Core;
using Ararem.RayTracer.Impl.Builtin;
using Eto.Forms;
using LibArarem.Core.Logging;
using Serilog;
using Serilog.Context;
using System;
using System.Diagnostics;

namespace Ararem.RayTracer.Display.Dev.UI;

/// <summary>
///  A <see cref="Panel"/> that exposes a GUI for an <see cref="Core.RenderJob"/>, with the ability to change the render options, selected scene, view
///  render statistics and view the render buffer
/// </summary>
public sealed partial class RenderJobPanel : Panel
{
	private readonly ILogger log;

	public RenderJobPanel()
	{
		log = LogUtils.WithInstanceContext(this);
	}

	//TODO: These should directly show the controls, without a surrounding GroupBox. Extract the groupbox code to this main class
	/// <summary>Panel that displays the render buffer of the current <see cref="RenderJob"/></summary>
	private RenderBufferPanel renderBufferPanel;

	/// <summary>Panel that controls the current <see cref="RenderJob"/> and it's render settings</summary>
	private RenderControllerPanel renderControllerPanel;

	/// <summary>Panel that displays the statistics of the current <see cref="RenderJob"/></summary>
	private RenderStatsPanel renderStatsPanel;

	/// <summary>Main <see cref="DynamicLayout"/> for this panel. Is the same as accessing <see cref="Panel.Content"/></summary>
	public DynamicLayout Layout { get; private set; }

	/// <summary>The current render job (if any)</summary>
	public RenderJob? RenderJob { get; private set; } = null;

	/// <summary>Currently selected scene. If a render is running, then it's the one that's being rendered.</summary>
	public Scene SelectedScene { get; private set; } = BuiltinScenes.Demo;

	/// <summary>Render options that affect how the <see cref="RenderJob"/> is rendered</summary>
	public RenderOptions RenderOptions { get; } = new();

	/// <inheritdoc/>
	protected override void OnPreLoad(EventArgs e)
	{
		log.TrackEvent(this,e);

		log.Verbose("Creating layout");
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
		Layout.Add(new GroupBox{Text = "Render Controls",Style=nameof(Force_Heading),ID = $"{ID}/Controller.GroupBox", Content = renderControllerPanel = new RenderControllerPanel(this) { ID = $"{ID}/Controller" }});
		Layout.Add(new GroupBox{Text = "Statistics",Style=nameof(Force_Heading),ID = $"{ID}/Stats.GroupBox", Content = renderStatsPanel = new RenderStatsPanel(this) { ID = $"{ID}/Stats" }});
		Layout.Add(new GroupBox{Text = "Preview",Style=nameof(Force_Heading),ID = $"{ID}/Buffer.GroupBox", Content = renderBufferPanel = new RenderBufferPanel(this) { ID = $"{ID}/Buffer" }});

		Layout.EndHorizontal();
		Layout.Create();
		log.Verbose("Layout created");

		base.OnPreLoad(e);
	}

	/// <inheritdoc/>
	protected override void Dispose(bool disposing)
	{
		//Dispose of all our child panels
		if (disposing)
		{
			renderBufferPanel.Dispose();
			renderControllerPanel.Dispose();
			renderStatsPanel.Dispose();
		}

		base.Dispose(disposing);
	}

	/// <summary>Updates all the UI</summary>
	public void UpdateUi()
	{
		//TODO: Add something to prevent UI freezes
		/*
		 * Note that we don't have to worry about locks or anything, since
		 * (A) - It's only called on the main thread, so that's thread safety done
		 * (B) - The timer is only ever reset *after* everything's already been updated, so no chance of two calls being executed at the same time
		 */
		Stopwatch         totalSw = Stopwatch.StartNew();
		log.Verbose("Updating panel");
		renderControllerPanel.Update();
		renderStatsPanel.Update();
		renderBufferPanel.Update();
		((DocumentPage)Parent).Invalidate();

		log.Verbose("Updated panel in {Elapsed:#00.000 'ms'}", totalSw.Elapsed.TotalMilliseconds);
	}
}