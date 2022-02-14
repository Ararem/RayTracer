using System.Numerics;

namespace RayTracer.Core.Textures;

/// <summary>
///  Base class for a texture
/// </summary>
public abstract class Texture
{
	/// <summary>
	///  Gets the colour value for a pixel, from UV and world-space coordinates
	/// </summary>
	/// <param name="uv">The UV coordinate</param>
	/// <param name="point">The world-space coordinate</param>
	/// <returns>The colour of the object at the location</returns>
	public abstract Colour GetColour(Vector2 uv, Vector3 point);
}