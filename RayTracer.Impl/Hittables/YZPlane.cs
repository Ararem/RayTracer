using RayTracer.Core;
using RayTracer.Core.Acceleration;
using System.Numerics;

namespace RayTracer.Impl.Hittables;

/// <summary>A plane that spans a region along the XY plane</summary>
public sealed class YZPlane : Hittable
{
	/// <summary>A plane that spans a region along the XY plane</summary>
	/// <param name="yLow">Low Y value for this plane</param>
	/// <param name="yHigh">High Y value for this plane</param>
	/// <param name="zLow">Low Z value for this plane</param>
	/// <param name="zHigh">High Z value for this plane</param>
	/// <param name="x">X value the plane is positioned at</param>
	/// <param name="aabbPadding">How much to pad the computed AABB by (since the plane is infinitely thin)</param>
	public YZPlane(float yLow, float yHigh, float zLow, float zHigh, float x, float aabbPadding = 0.001f)
	{
		YLow           = yLow;
		YHigh          = yHigh;
		ZLow           = zLow;
		ZHigh          = zHigh;
		X              = x;
		AABBPadding    = aabbPadding;
		BoundingVolume = new AxisAlignedBoundingBox(new Vector3(x - aabbPadding, yLow, zLow), new Vector3(x + aabbPadding, yHigh, zHigh));
	}

	/// <inheritdoc/>
	public override AxisAlignedBoundingBox BoundingVolume { get; }

	/// <summary>Low Y value for this plane</summary>
	public float YLow { get; init; }

	/// <summary>High Y value for this plane</summary>
	public float YHigh { get; init; }

	/// <summary>Low Z value for this plane</summary>
	public float ZLow { get; init; }

	/// <summary>High Z value for this plane</summary>
	public float ZHigh { get; init; }

	/// <summary>X value the plane is positioned at</summary>
	public float X { get; init; }

	/// <summary>How much to pad the computed AABB by (since the plane is infinitely thin)</summary>
	public float AABBPadding { get; init; }

	/// <inheritdoc/>
	public override HitRecord? TryHit(Ray ray, float kMin, float kMax)
	{
		//How far along the ray did it intersect with the unbounded version of this plane (bounds of +- infinity)
		float k = (X - ray.Origin.X) / ray.Direction.X;
		//The above code doesn't work when `ray.Direction.X == 0`, since the ray is essentially going along/parallel to the plane
		//So we have to do a sanity check here, otherwise `k` will be NaN, and that messes up everything else
		if ((ray.Direction.X == 0f) || float.IsNaN(k)) return null;
		if ((k < kMin) || (k > kMax)) //Out of range for our near/far plane
			return null;
		Vector3 worldPoint = ray.PointAt(k);
		float   y          = worldPoint.Y, z = worldPoint.Z;
		//Assert our bounds
		if ((y < YLow) || (y > YHigh) || (z < ZLow) || (z > ZHigh))
			return null;

		Vector3 centre     = new(X, (YLow + YHigh) / 2f, (ZLow + ZHigh) / 2f);
		Vector3 localPoint = worldPoint - centre;

		Vector2 uv = new((y - YLow) / (YHigh - YLow), (z - ZLow) / (ZHigh - ZLow));
		Vector3 outwardNormal =
				//See XYPlane.cs for explanation of this
				ray.Origin.X < X
						? -Vector3.UnitX
						: Vector3.UnitX;
		//Pretend front face is always true, since a 2D plane doesn't really have an 'inside'
		return new HitRecord(ray, worldPoint, localPoint, outwardNormal, k, true, uv);
	}

	/// <inheritdoc/>
	public override bool FastTryHit(Ray ray, float kMin, float kMax)
	{
		//How far along the ray did it intersect with the unbounded version of this plane (bounds of +- infinity)
		float k = (X - ray.Origin.X) / ray.Direction.X;
		//The above code doesn't work when `ray.Direction.X == 0`, since the ray is essentially going along/parallel to the plane
		//So we have to do a sanity check here, otherwise `k` will be NaN, and that messes up everything else
		if ((ray.Direction.X == 0f) || float.IsNaN(k)) return false;
		if ((k < kMin) || (k > kMax)) //Out of range for our near/far plane
			return false;
		Vector3 worldPoint = ray.PointAt(k);
		float   y          = worldPoint.Y, z = worldPoint.Z;
		//Assert our bounds
		return !(y < YLow) && !(y > YHigh) && !(z < ZLow) && !(z > ZHigh);
	}
}