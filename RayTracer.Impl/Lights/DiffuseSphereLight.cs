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
	{
		#pragma warning disable CS0162
		// ReSharper disable HeuristicUnreachableCode

		switch (1)
		{
			//Completely random
			case 0:
			{
				Vector3 randPos = Position + (RandUtils.RandomOnUnitSphere() * DiffusionRadius);

				return DefaultGetShadowRayForHit(hit.WorldPoint, randPos);
			}
			//Hemispherical random (facing towards the hit point)
			case 1:
			{
				Vector3 randDir                                                     = RandUtils.RandomOnUnitSphere();
				if (Dot(randDir, Normalize(hit.WorldPoint - Position)) < 0) randDir = -randDir; //Flip the random offset in case it's pointing away from the hit - this ensures it's on the closer side to the lit object
				Vector3 randPos                                                     = Position + (randDir * DiffusionRadius);

				return DefaultGetShadowRayForHit(hit.WorldPoint, randPos);
			}
			case 2:

		}

		// ReSharper restore HeuristicUnreachableCode
		#pragma warning restore CS0162
	}
}