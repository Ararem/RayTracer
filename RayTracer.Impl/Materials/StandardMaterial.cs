using RayTracer.Core;
using System.Numerics;
using static RayTracer.Core.RandUtils;
using static System.Numerics.Vector3;

namespace RayTracer.Impl.Materials;

/// <summary>A standard material that can be</summary>
/// <param name="Albedo">The albedo (colour) texture of this material</param>
/// <param name="Emission">The texture used for the light this material emits</param>
/// <param name="Diffusion">How 'diffuse' (random) the reflected rays are. Settings this to 0 means perfect reflections, 1 means completely diffuse</param>
public sealed record StandardMaterial(Texture Albedo, Texture Emission, float Diffusion) : IMaterial
{
	/// <inheritdoc/>
	public override Ray? Scatter(HitRecord hit)
	{
		Vector3 diffuse                           = RandomInUnitSphere(); //Pick a random scatter direction
		if (Dot(diffuse, hit.Normal) < 0) diffuse *= -1;                  //Ensure the resulting scatter is in the same direction as the normal (so it doesn't point inside the object)
		Vector3 reflect                           = Reflect(hit.Ray.Direction, hit.Normal);
		Vector3 scatter                           = Lerp(reflect, diffuse, Diffusion);

		// // Catch degenerate scatter direction (when scatter magnitude is almost 0)
		// const float thresh = (float)1e-5;
		// if ((scatter.X < thresh) && (scatter.Y < thresh) && (scatter.Z < thresh))
		// 	scatter = hit.Normal;

		Ray r = new(hit.WorldPoint, Normalize(scatter));
		return r;
	}

	/// <inheritdoc/>
	public override void DoColourThings(ref Colour colour, HitRecord hit, ArraySegment<(SceneObject sceneObject, HitRecord hitRecord)> previousHits) => colour = (colour * Albedo.GetColour(hit)) + Emission.GetColour(hit);
}