using JetBrains.Annotations;

namespace Ararem.RayTracer.Core;

//TODO: HDRI Skybox
/// <summary>
///  Base class that defines a skybox - what is seen at an infinite distance away from the world. This is essentially the "outside environment" that
///  appears as part of the <see cref="Scene"/>, but does not need to be directly rendered
/// </summary>
/// <remarks>
///  We essentially treat the skybox as an infinitely far away object (behind everything), and uses it to calculate sky lighting. The colour returned by
///  <see cref="SkyBox.GetSkyColour(RayTracer.Core.Ray)"/> is interpreted similar to the emissive colour of a material.
/// </remarks>
[PublicAPI]
public abstract class SkyBox
{
	/// <summary>Gets the sky colour that would be seen by a viewer looking along a certain <paramref name="ray"/></summary>
	/// <param name="ray">The ray corresponding to the view line of the viewer</param>
	public abstract Colour GetSkyColour(Ray ray);
}