using RayTracer.Core;
using Serilog;
using System.Numerics;
using System.Security.Cryptography;
using static System.MathF;
using static System.Numerics.Vector3;
using Log = Serilog.Log;

namespace RayTracer.Impl.Lights;

/// <inheritdoc cref="SimpleLightBase"/>
public class DiffuseSphereLight : SimpleLightBase
{
	/// <summary>How large of an area around the light's position should be considered as a valid point on the light's surface</summary>
	public float DiffusionRadius { get; init; }

	/// <inheritdoc/>
	protected override (Ray ray, float kMin, float kMax) GetShadowRayForHit(HitRecord hit)
	#if false
	//=> DefaultGetShadowRayForHit(hit.WorldPoint, Position + (RandUtils.RandomInUnitSphere() * DiffusionRadius));
	{
		Vector3 randDir                                          = RandUtils.RandomOnUnitSphere();
		if (Dot(randDir, hit.WorldPoint - Position) < 0) randDir = -randDir; //Flip the random offset in case it's pointing away from the hit - this ensures it's on the closer side to the lit object
		Vector3 randPos                                          = Position + (randDir * DiffusionRadius);

		return DefaultGetShadowRayForHit(randPos, hit.WorldPoint);
	}
	#elif true
	{
		Vector3 randDir       = RandUtils.RandomOnUnitSphere() * DiffusionRadius;
		Vector3 posToCheck    = Position   + randDir;
		Vector3 hitToCheckDir = posToCheck - hit.WorldPoint;

		//Flip the random offset in case it's pointing away from the hit's normal - this ensures it's on the closer side to the lit object
		//Here, we need to reflect it around the
		if (Dot(hitToCheckDir,  hit.Normal) < 0)
		{
			posToCheck = hit.WorldPoint + Reflect(hitToCheckDir, hit.Normal);
		}
		(Ray ray, float kMin, float kMax) rand = DefaultGetShadowRayForHit(hit.WorldPoint, posToCheck);

		// Vector3                           closestDir   = Normalize(hit.WorldPoint - Position);
		// Vector3                           closestPoint = Position + (closestDir * DiffusionRadius);
		// (Ray ray, float kMin, float kMax) closest      = DefaultGetShadowRayForHit(hit.WorldPoint, closestPoint);


		return rand;
	}
	#else
	{
		/*
		 * Special case handling here, kind-of a self-shadowing problem
		 *
		 *           P
		 *       _________
		 *      /         \
		 *     /           \
		 *   L |           |
		 *     \          /
		 *      \________/
		 *
		 * Here, the hit point `L` is being lit by the point `L` (L was randomly chosen). However, if we shadow check P->L, then we get an intersection (since the object itself is in the way)
		 * This makes it look really weird. To get around this, we do a quick check *before* we shadow check, to ensure the ray won't intersect with the object itself
		 * If it is self-intersecting, just retry with a new ray
		 */

		for (int i = 0; i < 1000; i++)
		{
			//Here choose a random point on the sphere
			Vector3 randUnitPoint = RandUtils.RandomOnUnitSphere();
			Vector3 lPos           = Position + (randUnitPoint * DiffusionRadius); //Point on our sphere light, world-space
			//Get the shadow ray between the hit and our random point on our light
			(Ray ray, float kMin, float kMax) = DefaultGetShadowRayForHit(hit.WorldPoint, lPos);
			//If there was no self-intersection, return the ray so we can shadow check in the scene
			//Otherwise the loop just repeats and tries again
			if (!hit.Hittable.FastTryHit(ray, kMin, kMax))
				return (ray, kMin, kMax);
		}

		//Fallback: check with the closest point on the sphere to the hit (this would give hard shadows but we don't care since  all else failed)
		return DefaultGetShadowRayForHit(hit.WorldPoint, Position);// with {kMax = Distance(hit.WorldPoint, Position) - DiffusionRadius};
	}
	#endif
}