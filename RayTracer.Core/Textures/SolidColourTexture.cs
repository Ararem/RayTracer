using RayTracer.Core.Hittables;

namespace RayTracer.Core.Textures;

/// <summary>
///  A texture that is a solid colour
/// </summary>
public class SolidColourTexture : TextureBase
{
	/// <summary>
	///  Creates a new texture from a colour
	/// </summary>
	public SolidColourTexture(Colour colour)
	{
		Colour = colour;
	}

	/// <summary>
	///  The colour of this texture
	/// </summary>
	public Colour Colour { get; }

	/// <inheritdoc/>
	public override Colour GetColour(HitRecord _) => Colour;
}