using System.Numerics;

namespace RayTracer.Core.Acceleration;

/// <summary>
///  A bounding volume that takes the shape of an axis aligned box (cuboid) that spans from <see cref="Min"/> to <see cref="Max"/> (these are two
///  opposing corners)
/// </summary>
/// <param name="Min">First corner of the box</param>
/// <param name="Max">Second corner of the box</param>
public sealed record AxisAlignedBoundingBox(Vector3 Min, Vector3 Max)
{
	/// <summary>An <see cref="AxisAlignedBoundingBox"/> that represents an unbounded AABB</summary>
	/// <remarks>
	///  This isn't truly infinite, as <see cref="float.PositiveInfinity"/> and <see cref="float.NegativeInfinity"/> may not work, however this is necessary
	///  for computation of <see cref="BvhTree">BVH Trees</see>
	/// </remarks>
	public static AxisAlignedBoundingBox Infinite { get; } = new(new Vector3(float.NegativeInfinity), new Vector3(float.PositiveInfinity));

	/// <summary>Tries to see if the given ray will intersect with the bounding box or not.</summary>
	/// <param name="ray">The ray to check for intersections along</param>
	/// <param name="kMin">Lower bound for distance along the ray</param>
	/// <param name="kMax">Upper bound for distance along the ray</param>
	/// <returns><see langword="true"/> if the ray intersects this bounding volume and may hit the object inside, else <see langword="false"/></returns>
	public bool Hit(Ray ray, float kMin, float kMax)
	{
		/*
		 * I did some optimisations based on profiling info, so theres a few ways of doing this
		 *
		 * (1): R# Suggested for loop
		 * (Vector3 ro, Vector3 rd) = ray;
		 * for (int a = 0; a < 3; a++)
		 * {
		 * 		float invD = 1.0f                           / Index(rd, a);
		 * 		float t0   = (Index(Min, a) - Index(ro, a)) * invD;
		 * 		float t1   = (Index(Max, a) - Index(ro, a)) * invD;
		 * 		if (invD < 0.0f)
		 * 			(t1, t0) = (t0, t1);
		 * 		kMin = t0 > kMin ? t0 : kMin;
		 * 		kMax = t1 < kMax ? t1 : kMax;
		 * 		if (kMax <= kMin)
		 * 			return false;
		 * }
		 *
		 * (2): No decomposition (since apparently it was 14% of access time??)
		 * for (int a = 0; a < 3; a++)
		 * {
		 * 		float invD = 1.0f                                   / Index(ray.Direction, a);
		 * 		float t0   = (Index(Min, a) - Index(ray.Origin, a)) * invD;
		 * 		float t1   = (Index(Max, a) - Index(ray.Origin, a)) * invD;
		 * 		if (invD < 0.0f)
		 * 			(t1, t0) = (t0, t1);
		 * 		kMin = t0 > kMin ? t0 : kMin;
		 * 		kMax = t1 < kMax ? t1 : kMax;
		 * 		if (kMax <= kMin)
		 * 			return false;
		 * }
		 *
		 * (3): Remove `Index()` method because its 30% of access time..???
		 */
		Vector3 invD = Vector3.One / ray.Direction;
		{
			float t0 = (Min.X - ray.Origin.X) * invD.X;
			float t1 = (Max.X - ray.Origin.X) * invD.X;
			if (invD.X < 0.0f)
				(t1, t0) = (t0, t1);
			kMin = t0 > kMin ? t0 : kMin;
			kMax = t1 < kMax ? t1 : kMax;
			if (kMax <= kMin)
				return false;
		}
		{
			float t0 = (Min.Y - ray.Origin.Y) * invD.Y;
			float t1 = (Max.Y - ray.Origin.Y) * invD.Y;
			if (invD.Y < 0.0f)
				(t1, t0) = (t0, t1);
			kMin = t0 > kMin ? t0 : kMin;
			kMax = t1 < kMax ? t1 : kMax;
			if (kMax <= kMin)
				return false;
		}
		{
			float t0 = (Min.Z - ray.Origin.Z) * invD.Z;
			float t1 = (Max.Z - ray.Origin.Z) * invD.Z;
			if (invD.Z < 0.0f)
				(t1, t0) = (t0, t1);
			kMin = t0 > kMin ? t0 : kMin;
			kMax = t1 < kMax ? t1 : kMax;
			if (kMax <= kMin)
				return false;
		}

		return true;
	}

	/// <summary>Returns an <see cref="AxisAlignedBoundingBox"/> that encompasses all the <paramref name="points"/></summary>
	/// <param name="points">Array of sub boxes to surround</param>
	/// <returns>An <see cref="AxisAlignedBoundingBox"/> whose volume contains all the <paramref name="points"/></returns>
	public static AxisAlignedBoundingBox Encompass(params Vector3[] points)
	{
		Vector3 min = new(float.PositiveInfinity), max = new(float.NegativeInfinity);
		foreach (Vector3 p in points)
		{
			min = Vector3.Min(min, p);
			max = Vector3.Max(max, p);
		}

		return new AxisAlignedBoundingBox(min, max);
	}

	/// <summary>
	/// Returns a copy of this instance with specified extra padding
	/// </summary>
	/// <param name="padding"></param>
	/// <returns></returns>
	public AxisAlignedBoundingBox WithPadding(float padding) => new (Min - new Vector3(padding), Max + new Vector3(padding));

	/// <summary>Returns an <see cref="AxisAlignedBoundingBox"/> that encompasses all the <paramref name="subBoxes"/></summary>
	/// <param name="subBoxes">Array of sub boxes to surround</param>
	/// <returns>An <see cref="AxisAlignedBoundingBox"/> whose volume contains all the <paramref name="subBoxes"/></returns>
	public static AxisAlignedBoundingBox Encompass(params AxisAlignedBoundingBox[] subBoxes)
	{
		if (subBoxes.Length < 2) throw new ArgumentException("Sub boxes must have at least 2 elements", nameof(subBoxes));
		Vector3 min = new(float.PositiveInfinity), max = new(float.NegativeInfinity);
		foreach (AxisAlignedBoundingBox aabb in subBoxes)
		{
			min = Vector3.Min(min, aabb.Min);
			max = Vector3.Max(max, aabb.Max);
		}

		return new AxisAlignedBoundingBox(min, max);
	}
}