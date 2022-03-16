using RayTracer.Core.Graphics;
using System.Numerics;

namespace RayTracer.Core.Hittables;

using static MathF;

public class YZPlane : Hittable
{
	private readonly Vector3 centre;

	public YZPlane(float yLow, float yHigh, float zLow, float zHigh, float x, float aabbPadding = 0.001f)
	{
		if (aabbPadding <= 0)
			throw new ArgumentOutOfRangeException(nameof(aabbPadding), aabbPadding, "Padding for the AABB must be > 0");
		X           = x;
		ZLow        = Min(zLow, zHigh);
		ZHigh       = Max(zLow, zHigh);
		YLow        = Min(yLow, yHigh);
		YHigh       = Max(yLow, yHigh);
		AABBPadding = aabbPadding;
		centre      = new Vector3(X, (YLow + YHigh) / 2f, (ZLow + ZHigh) / 2f); //Average high/low points
	}

	/// <summary>
	///  Low Y value for this plane
	/// </summary>
	public float YLow { get; }

	/// <summary>
	///  High Y value for this plane
	/// </summary>
	public float YHigh { get; }

	/// <summary>
	///  Low Z value for this plane
	/// </summary>
	public float ZLow { get; }

	/// <summary>
	///  High Z value for this plane
	/// </summary>
	public float ZHigh { get; }

	/// <summary>
	///  X value for this plane
	/// </summary>
	public float X { get; }

	/// <summary>
	///  How much to pad the computed AABB by (since the plane is infinitely thin)
	/// </summary>
	public float AABBPadding { get; }

	/// <inheritdoc/>
	public override HitRecord? TryHit(Ray ray, float kMin, float kMax)
	{
		//How far along the ray did it intersect with the unbounded version of this plane (bounds of +- infinity)
		float k = (X - ray.Origin.X) / ray.Direction.X;
		if ((k < kMin) || (k > kMax)) //Out of range for our near/far plane
			return null;
		Vector3 worldPoint = ray.PointAt(k);
		float   y          = worldPoint.Y, z = worldPoint.Z;
		//Assert our bounds
		if ((y < YLow) || (y > YHigh) || (z < ZLow) || (z > ZHigh))
			return null;

		Vector3 localPoint = worldPoint - centre;

		Vector2 uv = new((y - YLow) / (YHigh - YLow), (z - ZLow) / (ZHigh - ZLow));
		Vector3 outwardNormal =
				//See XYPlane.cs for explanation of this
				ray.Origin.X < X
						? new Vector3(-1, 0, 0)
						: new Vector3(1,  0, 0);
		//Pretend front face is always true, since a 2D plane doesn't really have an 'inside'
		return new HitRecord(ray, worldPoint, localPoint, outwardNormal, k, true, uv);
	}
}