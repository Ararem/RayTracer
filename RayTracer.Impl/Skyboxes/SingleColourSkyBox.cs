using JetBrains.Annotations;
using RayTracer.Core;

namespace RayTracer.Impl.Skyboxes;

/// <summary>
///  A skybox that is a single colour
/// </summary>
[PublicAPI]
public class SingleColourSkyBox : SkyBox
{
	/// <summary>
	///  A skybox that is a single colour
	/// </summary>
	/// <param name="colour">The colour of the sky</param>
	public SingleColourSkyBox(Colour colour)
	{
		Colour = colour;
	}

	/// <inheritdoc/>
	public override Colour GetSkyColour(Ray ray) => Colour;

	/// <summary>The colour of the sky</summary>
	public Colour Colour { get; init; }
}