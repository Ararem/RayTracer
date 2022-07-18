using Eto.Forms;
using RayTracer.Core;

namespace RayTracer.Display.Dev;

public class RenderJobTrackingTab : Control
{
	public RenderOptions RenderOptions { get; init; }
	public AsyncRenderJob RenderJob { get; }
}