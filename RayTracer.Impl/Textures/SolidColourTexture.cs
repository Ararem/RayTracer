using RayTracer.Core;

namespace RayTracer.Impl.Textures;

/// <summary>A texture that is a solid colour</summary>
public sealed class SolidColourTexture : Texture
{
	/// <summary>A texture that is a solid colour</summary>
	public SolidColourTexture(Colour colour)
	{
		Colour = colour;
	}

	/// <summary>
	/// Colour of the texture
	/// </summary>
	public Colour Colour { get; }

	/// <inheritdoc/>
	public override Colour GetColour(HitRecord _) => Colour;
}