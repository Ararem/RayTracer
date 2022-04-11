using JetBrains.Annotations;
using RayTracer.Core.Hittables;

namespace RayTracer.Core.Textures;

/// <summary>
///  Base class for a texture
/// </summary>
[PublicAPI]
public abstract record Texture
{
	/// <summary>
	///  Gets the colour value for a pixel
	/// </summary>
	/// <param name="hit">Information about the pixel</param>
	/// <returns>The colour of the object at the pixel</returns>
	public abstract Colour GetColour(HitRecord hit);

	/// <summary>
	///  Implicit operator to convert a colour to a solid texture of that colour
	/// </summary>
	public static implicit operator Texture(Colour colour) => new SolidColourTexture(colour);
}