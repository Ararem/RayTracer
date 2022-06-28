using RayTracer.Core;
using System.Numerics;
using static System.Numerics.Vector3;

namespace RayTracer.Impl.Lights;

/// <inheritdoc cref="SimpleLightBase"/>
public class DiffuseSphereLight : SimpleLightBase
{
	/// <summary>How large of an area around the light's position should be considered as a valid point on the light's surface</summary>
	public float DiffusionRadius { get; init; }

	/// <inheritdoc/>
	protected override (Ray ray, float kMin, float kMax) GetShadowRayForHit(HitRecord hit)
			#if !false
	//=> DefaultGetShadowRayForHit(hit.WorldPoint, Position + (RandUtils.RandomInUnitSphere() * DiffusionRadius));
	{
		Vector3 randDir = RandUtils.RandomOnUnitSphere();
		if (Dot(randDir, Normalize(hit.WorldPoint - Position)) < 0) randDir = -randDir; //Flip the random offset in case it's pointing away from the hit - this ensures it's on the closer side to the lit object
		Vector3 randPos = Position + (randDir * DiffusionRadius);

		return DefaultGetShadowRayForHit(hit.WorldPoint, randPos);
	}
			#elif true
	{
		Vector3 h                                                = hit.WorldPoint;
		Vector3 randDir                                          = RandUtils.RandomOnUnitSphere() * DiffusionRadius;
		// if (Dot(randDir, hit.WorldPoint - Position) < 0) randDir = -randDir;           //Flip the random offset in case it's pointing away from the hit - this ensures it's on the closer side to the lit object
		Vector3 l                                                = Position + randDir; //Random point on the surface of our sphere light (gonna shadow check this)
		Vector3 hToL                                             = l        - h;       //The sized direction vector that goes from the hit towards the point `l`

		/*
		 * In case the direction towards the point we're checking (aka the shadow ray direction) is 'behind' the hit (aka away from the normal)
		 * Then we flip the point around so that it's going with the normal
		 * Helps resolve these weird self-shadowing issues (see below - the path H->L is behind the normal, so flip it to position F)
		 *    |     L
		 *    |  _________
		 *    | /         \
		 *    |/           \
		 *<--H|            |
		 *    |\          /
		 *    | \________/
		 * F  |
		 *
		 * Since this causes some weird backface lighting when the hit is outside the radius of the diffusion, just check the distance and only flip if the point is inside the sphere's radius
		 */
		if ((Distance(h, Position) < DiffusionRadius) && (Dot(hToL, hit.Normal) < 0))
		{
			Vector3 hToF = true?-hToL:Reflect(hToL, hit.Normal); //Flip
			// l = h + hToF;                                   //Recalculate new position
		}

		(Ray ray, float kMin, float kMax) rand = DefaultGetShadowRayForHit(hit.WorldPoint, l);

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
			Vector3 lPos = Position + (randUnitPoint * DiffusionRadius); //Point on our sphere light, world-space
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