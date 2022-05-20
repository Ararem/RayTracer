namespace RayTracer.Core;

//TODO: HDRI Skybox
/// <summary>Interface a skybox</summary>
/// <remarks>
///  We essentially treat the skybox as an infinitely far away object (behind everything), and uses it to calculate sky lighting. The colour returned by
///  <see cref="SkyBox.GetSkyColour(RayTracer.Core.Ray)"/> is interpreted as the emissive colour of a material.
/// </remarks>
public abstract class SkyBox
{
	/// <summary>Gets the sky colour that would be seen by a viewer looking along a certain <paramref name="ray"/></summary>
	/// <param name="ray">The ray corresponding to the view line of the viewer</param>
	public abstract Colour GetSkyColour(Ray ray);
}