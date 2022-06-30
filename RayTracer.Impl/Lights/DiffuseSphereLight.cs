using RayTracer.Core;
using System.Numerics;
using static System.Numerics.Vector3;
using static System.MathF;
using Log = Serilog.Log;

namespace RayTracer.Impl.Lights;

/// <inheritdoc cref="SimpleLightBase"/>
public class DiffuseSphereLight : SimpleLightBase
{
	/// <summary>How large of an area around the light's position should be considered as a valid point on the light's surface</summary>
	public float DiffusionRadius { get; init; }

	/// <inheritdoc/>
	protected override (Ray ray, float kMin, float kMax) GetShadowRayForHit(HitRecord hit)
	{
		switch (4)
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
			{
				//Choose a random point inside our sphere light's radius
				// Vector3 randPos                                                     = Position + (RandUtils.RandomOnUnitSphere() * DiffusionRadius);
				Vector3 randDir                                                     = RandUtils.RandomOnUnitSphere();
				if (Dot(randDir, Normalize(hit.WorldPoint - Position)) < 0) randDir = -randDir; //Flip the random offset in case it's pointing away from the hit - this ensures it's on the closer side to the lit object
				Vector3 randPos                                                     = Position + (randDir * DiffusionRadius);

				//Ray that links the hit to our random point
				Ray ray = Ray.FromPoints(hit.WorldPoint, randPos);

				//Do some ray-sphere intersection math to find if the ray intersects
				Vector3 rayPos = ray.Origin, rayDir = ray.Direction;
				Vector3 oc     = rayPos - Position;
				float   a      = rayDir.LengthSquared();
				float   halfB  = Dot(oc, rayDir);
				float   c      = oc.LengthSquared() - (DiffusionRadius * DiffusionRadius * 1.001f);

				float discriminant                           = (halfB * halfB) - (a * c);
				if (Abs(discriminant) < 0.001f) discriminant = 0f;
				if (discriminant      < 0) throw new NotFiniteNumberException("Discriminant is -ve", discriminant); //No solutions to where ray intersects with sphere because of negative square root

				float sqrtD = Sqrt(discriminant);

				// Find the nearest root that lies in the acceptable range.
				float   k                   = (-halfB - sqrtD) / a;
				Vector3 closestPointOnLight = ray.PointAt(k - 10f); //Closest point on the surface of the sphere that we hit (world space)

				return DefaultGetShadowRayForHit(hit.WorldPoint, closestPointOnLight);
			}
			case 3:
			{
				for (int i = 0; i < 1000; i++)
				{
					//Here choose a random point on the sphere
					Vector3 randUnitPoint = RandUtils.RandomOnUnitSphere();
					Vector3 lPos          = Position + (randUnitPoint * DiffusionRadius); //Point on our sphere light, world-space
					//Get the shadow ray between the hit and our random point on our light
					(Ray ray, float kMin, float kMax) = DefaultGetShadowRayForHit(hit.WorldPoint, lPos);
					//If there was no self-intersection, return the ray so we can shadow check in the scene
					//Otherwise the loop just repeats and tries again
					if (!hit.Hittable.FastTryHit(ray, kMin, kMax))
					{
						if ((RandUtils.RandomFloat01() < 0.01f) && (i != 0) && (i > 10)) Log.Verbose("{I}", i);
						return (ray, kMin, kMax);
					}
				}

				//Fallback: check with the closest point on the sphere to the hit (this would give hard shadows but we don't care since  all else failed)
				return DefaultGetShadowRayForHit(hit.WorldPoint, Position); // with {kMax = Distance(hit.WorldPoint, Position) - DiffusionRadius};
			}
			case 4:
			{
				if (Distance(hit.WorldPoint, Position) <= DiffusionRadius * 1.01f) //Special handling for when the hit is very close to (or inside) the light
				{
					for (int i = 0; i < 1000; i++)
					{
						//Choose a random direction vector
						Vector3 randDir = RandUtils.RandomOnUnitSphere();
						//Ensure that it's pointing (roughly) in the same direction as the hit's normal
						if (Dot(randDir, hit.Normal) < 0) randDir = -randDir;

						//Now we have that vector, try and find the closest point on our sphere along it
						{
							//Do some ray-sphere intersection math to find if the ray intersects
							Vector3 rayPos = hit.WorldPoint;
							Vector3 oc     = rayPos - Position;
							float   a      = randDir.LengthSquared();
							float   halfB  = Dot(oc, randDir);
							float   c      = oc.LengthSquared() - (DiffusionRadius * DiffusionRadius);

							float discriminant = (halfB * halfB) - (a * c);
							if (discriminant < 0) goto Fail; //No solutions to where ray intersects with sphere because of negative square root

							float sqrtD = Sqrt(discriminant);

							// Find the nearest root that lies in the acceptable range.
							//This way we do a double check on both, prioritizing the less-positive root (as it's closer)
							//And we only return null if neither is valid
							float k = (-halfB - sqrtD) / a;
							if (k is float.NaN)
							{
								k = (-halfB + sqrtD) / a;
								if (k is float.NaN) goto Fail;
							}

							return (new Ray(hit.WorldPoint, randDir), 0.001f, k);
						}


					Fail: ;
					}
				}
				else
				{
					Vector3 randDir                                                     = RandUtils.RandomOnUnitSphere();
					if (Dot(randDir, Normalize(hit.WorldPoint - Position)) < 0) randDir = -randDir; //Flip the random offset in case it's pointing away from the hit - this ensures it's on the closer side to the lit object
					Vector3 randPos                                                     = Position + (randDir * DiffusionRadius);

					return DefaultGetShadowRayForHit(hit.WorldPoint, randPos);
				}

				//Fallback: check with the closest point on the sphere to the hit (this would give hard shadows but we don't care since  all else failed)
				if (RandUtils.RandomFloat01() < 0.0001f) Log.Verbose("Fallback");
				return DefaultGetShadowRayForHit(hit.WorldPoint, Position); // with {kMax = Distance(hit.WorldPoint, Position) - DiffusionRadius};
			}
		}
	}
}