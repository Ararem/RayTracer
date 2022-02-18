using RayTracer.Core.Hittables;

namespace RayTracer.Core.Textures;

/// <summary>
///  A texture that is a solid colour
/// </summary>
public sealed record SolidColourTexture(Colour Colour) : TextureBase
{
	/// <inheritdoc/>
	public override Colour GetColour(HitRecord _) => Colour;
}