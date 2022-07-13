using RayTracer.Core;

namespace RayTracer.Display.Dev;

public class RenderJobTrackingTab
{
	private RenderJobTrackingTab(AsyncRenderJob renderJob)
	{
	}

	/// <summary>Gets the render options</summary>
	public RenderOptions RenderOptions => RenderJob.RenderOptions;

	public AsyncRenderJob RenderJob { get; }
}