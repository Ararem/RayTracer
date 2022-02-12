using JetBrains.Annotations;
using RayTracer.Core;
using RayTracer.Core.Graphics;
using RayTracer.Core.Hittables;
using RayTracer.Core.Materials;
using System.Numerics;

namespace OLDRayTracer.Materials;

/// <summary>
///  A material that has diffuse-like properties.
/// </summary>
public sealed class DiffuseMaterial : Material
{
	/// <summary>
	///  Creates a new <see cref="DiffuseMaterial"/> from an <paramref name="colour"/> colour
	/// </summary>
	public DiffuseMaterial(Colour colour)
	{
		Colour = colour;
	}

	/// <summary>
	///  The albedo (colour) of this material
	/// </summary>
	[PublicAPI]
	public Colour Colour { get; }

	/// <inheritdoc/>
	public override Ray? Scatter(HitRecord hit)
	{
		Vector3 scatter = Rand.RandomInUnitSphere(); //Pick a random scatter direction
		scatter = Vector3.Dot(scatter, hit.Normal) > 0
				? scatter
				: -scatter; //Ensure the resulting scatter is in the same direction as the normal
		// scatter += meshHit.Normal;

		// Catch degenerate scatter direction
		const float thresh = (float)1e-5;
		if ((scatter.X < thresh) && (scatter.Y < thresh) && (scatter.Z < thresh))
			scatter = hit.Normal;

		Vector3 point = hit.Point;
		Ray     r     = new(point, Vector3.Normalize(scatter));
		return r;
	}

	/// <inheritdoc/>
	public override void DoColourThings(ref Colour colour, HitRecord hit, int bounces) => colour *= Colour;
}