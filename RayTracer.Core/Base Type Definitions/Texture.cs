using JetBrains.Annotations;

namespace RayTracer.Core;

/// <summary>
///  Base class for a texture
/// </summary>
[PublicAPI]
public abstract class Texture
{
	/// <summary>
	///  Gets the colour value for a pixel
	/// </summary>
	/// <param name="hit">Information about the pixel</param>
	/// <returns>The colour of the object at the pixel</returns>
	public abstract Colour GetColour(HitRecord hit);
}