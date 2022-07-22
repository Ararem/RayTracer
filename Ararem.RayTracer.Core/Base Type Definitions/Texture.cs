using JetBrains.Annotations;

namespace Ararem.RayTracer.Core;

/// <summary>Base class for a texture. </summary>
//TODO: Rewrite this to be generic: Vec2 for uv, Vec3 for world-space, etc
[PublicAPI]
public abstract class Texture
{
	/// <summary>Gets the colour value for a pixel</summary>
	/// <param name="hit">Information about the pixel</param>
	/// <returns>The colour of the object at the pixel</returns>
	public abstract Colour GetColour(HitRecord hit);
}