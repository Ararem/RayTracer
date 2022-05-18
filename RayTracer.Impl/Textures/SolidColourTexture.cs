using RayTracer.Core;

namespace RayTracer.Impl.Textures;

/// <summary>A texture that is a solid colour</summary>
public sealed record SolidColourTexture(Colour Colour) : Texture
{
	/// <inheritdoc/>
	public override Colour GetColour(HitRecord _) => Colour;
}