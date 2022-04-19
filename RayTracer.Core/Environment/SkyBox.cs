using JetBrains.Annotations;

namespace RayTracer.Core.Environment;
//TODO: HDRI Skybox
/// <summary>
///  Base class for implementing a skybox
/// </summary>
/// <remarks>
///  We essentially treat the skybox as an infinitely far away object (behind everything), and uses it to calculate sky lighting. The colour returned by
///  <see cref="GetSkyColour"/> is interpreted as the emissive colour of a material.
/// </remarks>
[PublicAPI]
public abstract record SkyBox
{
	/// <summary>
	///  Gets the sky colour that would be seen by a viewer looking along a certain <paramref name="ray"/>
	/// </summary>
	/// <param name="ray">The ray corresponding to the view line of the viewer</param>
	public abstract Colour GetSkyColour(Ray ray);
}