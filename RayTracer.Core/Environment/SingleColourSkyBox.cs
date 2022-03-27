using RayTracer.Core.Graphics;

namespace RayTracer.Core.Environment;

/// <summary>
/// A skybox that is a single colour
/// </summary>
/// <param name="Colour">The colour of the sky</param>
public record SingleColourSkyBox(Colour Colour) : SkyBox
{
	/// <inheritdoc />
	public override Colour GetSkyColour(Ray ray) => Colour;
}