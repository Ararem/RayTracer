using System.Numerics;

namespace RayTracer.Core.Acceleration;

public sealed record AABB(Vector3 Min, Vector3 Max) : BoundingVolume
{
	/// <inheritdoc />
	public override bool Hit(Ray ray, float kMin, float kMax)
	{
		for(int a = 0; a<3; a++)
		{
			float invD = 1.0f / Index(ray.Direction,a);
			float   t0   = (Index(Min, a) - Index(ray.Origin, a)) * invD;
			float   t1   = (Index(Max, a) - Index(ray.Origin, a)) * invD;
			if (invD < 0.0f)
				(t1,t0)=(t0, t1);
			kMin = t0 > kMin ? t0 : kMin;
			kMax = t1 < kMax ? t1 : kMax;
			if (kMax <= kMin)
				return false;
		}

		return true;

		static float Index(Vector3 v, int p)
		{
			// ReSharper disable once ConvertSwitchStatementToSwitchExpression
			switch (p)
			{
				case 0:  return v.X;
				case 1:  return v.Y;
				case 2:  return v.Z;
				default: throw new ArgumentOutOfRangeException(nameof(p), p, "Invalid vector index");
			}
		}
	}
}