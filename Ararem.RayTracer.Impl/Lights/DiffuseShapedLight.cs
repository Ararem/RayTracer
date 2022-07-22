using Ararem.RayTracer.Core;
using Serilog;
using System.Numerics;

namespace Ararem.RayTracer.Impl.Lights;

public sealed class DiffuseShapedLight : SimpleLightBase
{
	public DiffuseShapedLight(Hittable shape)
	{
		Shape = shape;
	}

	/// <summary>The shape of this light</summary>
	public Hittable Shape { get; }

	private (Vector3 p1, Vector3 p2) ChoosePointsForIntersection()
	{
		//Choose two random points on the surface of the bounding box of the shape
		Vector3 p1,                              p2;
		Vector3 min  = Shape.BoundingVolume.Min, max = Shape.BoundingVolume.Max;
		int     rand = RandUtils.RandomInt(0, 6);
		switch (rand)
		{
			case 0:
			case 1:
				p1.X = min.X;
				p2.X = max.X;

				if (rand == 0) (p1.X, p2.X) = (p2.X, p1.X); //Maybe swap

				p1.Y = RandUtils.RandomFloat(min.Y, max.Y);
				p2.Y = RandUtils.RandomFloat(min.Y, max.Y);
				p1.Z = RandUtils.RandomFloat(min.Z, max.Z);
				p2.Z = RandUtils.RandomFloat(min.Z, max.Z);
				break;

			case 2:
			case 3:
				p1.Y = min.Y;
				p2.Y = max.Y;

				if (rand == 2) (p1.X, p2.X) = (p2.Y, p1.Y); //Maybe swap

				p1.X = RandUtils.RandomFloat(min.X, max.X);
				p2.X = RandUtils.RandomFloat(min.X, max.X);
				p1.Z = RandUtils.RandomFloat(min.Z, max.Z);
				p2.Z = RandUtils.RandomFloat(min.Z, max.Z);
				break;
			case 4:
			case 5:
				p1.Z = min.Z;
				p2.Z = max.Z;

				if (rand == 4) (p1.Z, p2.Z) = (p2.Z, p1.Z); //Maybe swap

				p1.Y = RandUtils.RandomFloat(min.Y, max.Y);
				p2.Y = RandUtils.RandomFloat(min.Y, max.Y);
				p1.X = RandUtils.RandomFloat(min.X, max.X);
				p2.X = RandUtils.RandomFloat(min.X, max.X);
				break;
			default: throw new ArgumentOutOfRangeException(nameof(rand), rand, "Invalid random number was returned when choosing axis");
		}

		return (p1, p2);
	}

	/// <inheritdoc/>
	protected override (Ray ray, float kMin, float kMax) GetShadowRayForHit(HitRecord hit)
	{
		//Essentially, we try to find a ray that goes between the object hit, and the light source, one that we know should intersect with the light
		//We make sure that it does intersect with the light here in the for loop (otherwise what's the point of shadow checking it)
		//Then we pass it back to the base class which handles the shadow checks and attenuation calculations
		//This also ensures that the point we're trying to shadow check is the closest point to the lit object, otherwise we can get weird shadowing artifacts

		//TODO: Perhaps keep a list of known fallback points just in case?

		for (int i = 0; i < 1000; i++) //Loop until we get a valid hit, or exceed the threshold
		{
			//We choose two points, one being the origin of the hit on the material, and the other being a point in the AABB of the light's shape
			Vector3 p1 = hit.WorldPoint;
			//TODO: Problems when the light AABB is inside the lit object's aabb
			Vector3 p2 = new( //TODO: Rand.UnitCube?
					MathUtils.Lerp(Shape.BoundingVolume.Min.X, Shape.BoundingVolume.Max.X, RandUtils.RandomFloat01()),
					MathUtils.Lerp(Shape.BoundingVolume.Min.Y, Shape.BoundingVolume.Max.Y, RandUtils.RandomFloat01()),
					MathUtils.Lerp(Shape.BoundingVolume.Min.Z, Shape.BoundingVolume.Max.Z, RandUtils.RandomFloat01())
			);
			Ray r = Ray.FromPoints(p1, p2); //The ray goes [hit object's point] ==> [point inside AABB]

			//I don't think the bounds matter too much
			const float shapeKMin = 0.0001f;                //These two are only for finding a point on the surface of the light
			const float shapeKMax = float.PositiveInfinity; //And have nothing to do with intersections in the scene being rendered

			//Try intersecting with the object to see if the ray hits the light source
			HitRecord? maybeShapeHit = Shape.TryHit(r, shapeKMin, shapeKMax);
			if (maybeShapeHit is {} lightSourceHit)
			{
				//There was an intersection, so the ray hits the light source
				//So return the ray
				// if(RandUtils.RandomFloat01() < 0.0001f && i!=0) Log.Verbose("Intersected after {I} iterations", i);
				return (r, 0.001f, lightSourceHit.K - 0.001f); //Check for shadows for k = [0.001..where the light source was hit]
			}
		}

		Log.Verbose("Failed to find intersection");
		return (new Ray(hit.WorldPoint, Vector3.One), 0f, float.PositiveInfinity); //Fallback
	}
}