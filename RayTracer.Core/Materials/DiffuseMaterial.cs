using RayTracer.Core.Graphics;
using RayTracer.Core.Hittables;
using RayTracer.Core.Textures;
using System.Numerics;

namespace RayTracer.Core.Materials;

/// <summary>
///  A material that has diffuse-like properties.
/// </summary>
/// <param name="Texture">
///  The albedo (colour) texture of this material
/// </param>
public sealed record DiffuseMaterial(TextureBase Texture) : MaterialBase
{
	/// <inheritdoc/>
	public override Ray? Scatter(HitRecord hit)
	{
		Vector3 scatter = Rand.RandomInUnitSphere();   //Pick a random scatter direction
		scatter = Vector3.Dot(scatter, hit.Normal) > 0 //Ensure the resulting scatter is in the same direction as the normal (so it doesn't point inside the object)
				? scatter
				: -scatter;

		// Catch degenerate scatter direction (when scatter magnitude is almost 0)
		const float thresh = (float)1e-5;
		if ((scatter.X < thresh) && (scatter.Y < thresh) && (scatter.Z < thresh))
			scatter = hit.Normal;

		Ray r = new(hit.WorldPoint, Vector3.Normalize(scatter));
		return r;
	}

	/// <inheritdoc/>
	public override void DoColourThings(ref Colour colour, HitRecord hit, int bounces) => colour *= Texture.GetColour(hit);
}