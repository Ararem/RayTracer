using RayTracer.Core.Graphics;

namespace RayTracer.Core.Environment;

/// <summary>
///  Base class for implementing a skybox
/// </summary>
public abstract class SkyBox
{
	/// <summary>
	///  Gets the sky colour that would be seen by a viewer looking along a certain <paramref name="ray"/>
	/// </summary>
	/// <param name="ray">The ray corresponding to the view line of the viewer</param>
	public abstract Colour GetSkyColour(Ray ray);
}