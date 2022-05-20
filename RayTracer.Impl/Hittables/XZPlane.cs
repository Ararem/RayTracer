using JetBrains.Annotations;
using RayTracer.Core;
using RayTracer.Core.Acceleration;
using System.Numerics;

namespace RayTracer.Impl.Hittables;

/// <summary>A plane that spans a region along the XZ plane</summary>
public sealed class XZPlane : Hittable
{
	private readonly Vector3 centre;

	/// <summary>A plane that spans a region along the XZ plane</summary>
	/// <param name="xLow">Low X value for this plane</param>
	/// <param name="xHigh">High X value for this plane</param>
	/// <param name="zLow">Low Z value for this plane</param>
	/// <param name="zHigh">High Z value for this plane</param>
	/// <param name="y">Y value the plane is positioned at</param>
	/// <param name="aabbPadding">How much to pad the computed AABB by (since the plane is infinitely thin)</param>
	public XZPlane(float xLow, float xHigh, float zLow, float zHigh, float y, float aabbPadding = 0.001f)
	{
		XLow           = xLow;
		XHigh          = xHigh;
		ZLow           = zLow;
		ZHigh          = zHigh;
		Y              = y;
		AABBPadding    = aabbPadding;
		BoundingVolume = new AxisAlignedBoundingBox(new Vector3(xLow, y - aabbPadding, zLow), new Vector3(xHigh, y + aabbPadding, zHigh));
		centre         = new Vector3((XLow + XHigh) / 2f, Y, (ZLow + ZHigh) / 2f);
	}

	/// <inheritdoc/>
	public override AxisAlignedBoundingBox BoundingVolume { get; }

	/// <summary>Low X value for this plane</summary>
	[PublicAPI]
public float XLow { get; }

	/// <summary>High X value for this plane</summary>
	[PublicAPI]
public float XHigh { get; }

	/// <summary>Low Z value for this plane</summary>
	[PublicAPI]
public float ZLow { get; }

	/// <summary>High Z value for this plane</summary>
	[PublicAPI]
public float ZHigh { get; }

	/// <summary>Y value the plane is positioned at</summary>
	[PublicAPI]
public float Y { get; }

	/// <summary>How much to pad the computed AABB by (since the plane is infinitely thin)</summary>
	[PublicAPI]
public float AABBPadding { get; }

	/// <inheritdoc/>
	public override HitRecord? TryHit(Ray ray, float kMin, float kMax)
	{
		//How far along the ray did it intersect with the unbounded version of this plane (bounds of +- infinity)
		float k = (Y - ray.Origin.Y) / ray.Direction.Y;
		//The above code doesn't work when `ray.Direction.Y == 0`, since the ray is essentially going along/parallel to the plane
		//So we have to do a sanity check here, otherwise `k` will be NaN, and that messes up everything else
		if ((ray.Direction.Y == 0f) || float.IsNaN(k)) return null;
		if ((k < kMin) || (k > kMax)) //Out of range for our near/far plane
			return null;
		Vector3 worldPoint = ray.PointAt(k);
		float   x          = worldPoint.X, z = worldPoint.Z;
		//Assert our bounds
		if ((x < XLow) || (x > XHigh) || (z < ZLow) || (z > ZHigh))
			return null;

		Vector3 localPoint = worldPoint - centre;

		Vector2 uv = new((x - XLow) / (XHigh - XLow), (z - ZLow) / (ZHigh - ZLow));
		Vector3 outwardNormal =
				//See XYPlane.cs for explanation of this
				ray.Origin.Y < Y
						? -Vector3.UnitY
						: Vector3.UnitY;
		//Pretend front face is always true, since a 2D plane doesn't really have an 'inside'
		return new HitRecord(ray, worldPoint, localPoint, outwardNormal, k, true, uv);
	}

	/// <inheritdoc/>
	public override bool FastTryHit(Ray ray, float kMin, float kMax)
	{
		//How far along the ray did it intersect with the unbounded version of this plane (bounds of +- infinity)
		float k = (Y - ray.Origin.Y) / ray.Direction.Y;
		//The above code doesn't work when `ray.Direction.Y == 0`, since the ray is essentially going along/parallel to the plane
		//So we have to do a sanity check here, otherwise `k` will be NaN, and that messes up everything else
		if ((ray.Direction.Y == 0f) || float.IsNaN(k)) return false;
		if ((k < kMin) || (k > kMax)) //Out of range for our near/far plane
			return false;
		Vector3 worldPoint = ray.PointAt(k);
		float   x          = worldPoint.X, z = worldPoint.Z;
		//Assert our bounds
		return !(x < XLow) && !(x > XHigh) && !(z < ZLow) && !(z > ZHigh);
	}
}