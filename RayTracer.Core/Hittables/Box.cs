using System.Numerics;
using static System.MathF;

namespace RayTracer.Core.Hittables;

public record Box(Vector3 Min, Vector3 Max) : Hittable
{
	private readonly Lazy<Vector3> centre = new(() => (Min + Max) / 2f);

	/// <inheritdoc />
	public override HitRecord? TryHit(Ray ray, float kMin, float kMax)
	{
		//This is slightly modified code from the ray intersection of an AABB
		float   k =float.PositiveInfinity;
		Vector3 normal;
		{
			float invD = 1.0f                         / ray.Direction.X;
			float t0   = (Min.X - ray.Origin.X) * invD;
			float t1   = (Max.X - ray.Origin.X) * invD;
			if (invD < 0.0f)
				(t0, t1) = (t1, t0); //Swap
			float tMin = t0 > kMin ? t0 : kMin;
			float tMax = t1 < kMax ? t1 : kMax;
			if (tMax <= tMin)
				return null;
			k = Min(k, tMin);
			normal = ray.Origin.X <
		}
		{
			float invD = 1.0f                         / ray.Direction.Y;
			float t0   = (Min.Y - ray.Origin.Y) * invD;
			float t1   = (Max.Y - ray.Origin.Y) * invD;
			if (invD < 0.0f)
				(t0, t1) = (t1, t0); //Swap
			float tMin = t0 > kMin ? t0 : kMin;
			float tMax = t1 < kMax ? t1 : kMax;
			if (tMax <= tMin)
				return null;
			k = Min(k, tMin);
		}
		{
			float invD = 1.0f                         / ray.Direction.Z;
			float t0   = (Min.Z - ray.Origin.Z) * invD;
			float t1   = (Max.Z - ray.Origin.Z) * invD;
			if (invD < 0.0f)
				(t0, t1) = (t1, t0); //Swap
			float tMin = t0 > kMin ? t0 : kMin;
			float tMax = t1 < kMax ? t1 : kMax;
			if (tMax <= tMin)
				return null;
			k = Min(k, tMin);
		}

		Vector3 worldPoint = ray.PointAt(k);
		Vector3 localPoint      = worldPoint - centre.Value;

		return new HitRecord(ray, worldPoint, localPoint,);
	}
}