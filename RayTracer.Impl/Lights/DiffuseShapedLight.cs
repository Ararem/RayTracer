using RayTracer.Core;
using Serilog;
using System.Numerics;
using System.Security.Cryptography;

namespace RayTracer.Impl.Lights;

public class DiffuseShapedLight : SimpleLightBase
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
			default: throw new ArgumentOutOfRangeException(nameof(rand),rand, "Invalid random number was returned when choosing axis");
		}

		return (p1, p2);
	}

	/// <inheritdoc/>
	public override Vector3 ChooseIntersectTestPosition(HitRecord hit)
	{
		//Loop until we get a valid hit, or exceed the threshold
		for (int i = 0; i < 1000; i++)
		{
			//Choose points, then get ray and distance from them
			(Vector3 p1, Vector3 p2) = ChoosePointsForIntersection();
			Ray         r    = Ray.FromPoints(p1, p2);
			const float kMin = 0.0001f;
			float       kMax = Vector3.Distance(p1, p2);

			//Try intersecting
			HitRecord? maybeShapeHit = Shape.TryHit(r, kMin, kMax);
			if (maybeShapeHit is not null)
			{
				// if(RandUtils.RandomFloat01() < 0.00001f) Log.Verbose("Intersected after {I} iterations", i);
				return maybeShapeHit.Value.WorldPoint;
			}
		}

		Log.Verbose("Failed to find intersection");
		return Position; //Fallback
	}
}