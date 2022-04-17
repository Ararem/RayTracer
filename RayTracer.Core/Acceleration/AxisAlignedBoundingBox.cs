using System.Numerics;

namespace RayTracer.Core.Acceleration;

/// <summary>
/// A bounding volume that takes the shape of an axis aligned box (cuboid) that spans from <see cref="Min"/> to <see cref="Max"/> (these are two opposing corners)
/// </summary>
/// <param name="Min">First corner of the box</param>
/// <param name="Max">Second corner of the box</param>
public sealed record AxisAlignedBoundingBox(Vector3 Min, Vector3 Max) : BoundingVolume
{
	/// <inheritdoc />
	public override bool Hit(Ray ray, float kMin, float kMax)
	{
		(Vector3 ro, Vector3 rd) = ray;
		for(int a = 0; a<3; a++)
		{
			float invD        = 1.0f                                   / Index(rd, a);
			float t0          = (Index(Min, a) - Index(ro, a)) * invD;
			float t1          = (Index(Max, a) - Index(ro, a)) * invD;
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

	/// <summary>
	/// Returns an <see cref="AxisAlignedBoundingBox"/> that encompasses all the <paramref name="subBoxes"/>
	/// </summary>
	/// <param name="subBoxes">Array of sub boxes to surround</param>
	/// <returns>An <see cref="AxisAlignedBoundingBox"/> whose volume contains all the <see cref="subBoxes"/></returns>
	public static AxisAlignedBoundingBox Encompass(params AxisAlignedBoundingBox[] subBoxes)
	{
		Vector3 min = new(float.PositiveInfinity), max = new(float.NegativeInfinity);
		foreach (AxisAlignedBoundingBox aabb in subBoxes)
		{
			min = Vector3.Min(min, aabb.Min);
			max = Vector3.Max(max, aabb.Max);
		}

		return new AxisAlignedBoundingBox(min, max);
	}
}