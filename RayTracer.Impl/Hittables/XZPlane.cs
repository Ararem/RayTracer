using RayTracer.Core;
using RayTracer.Core.Acceleration;
using System.Numerics;

namespace RayTracer.Impl.Hittables;

/// <summary>
///  A plane that spans a region along the XZ plane
/// </summary>
/// <param name="XLow">Low X value for this plane</param>
/// <param name="XHigh">High X value for this plane</param>
/// <param name="ZLow">Low Z value for this plane</param>
/// <param name="ZHigh">High Z value for this plane</param>
/// <param name="Y">Y value the plane is positioned at</param>
/// <param name="AABBPadding">How much to pad the computed AABB by (since the plane is infinitely thin)</param>
public sealed record XZPlane(float XLow, float XHigh, float ZLow, float ZHigh, float Y, float AABBPadding = 0.001f) : Hittable
{
	/// <inheritdoc/>
	public override AxisAlignedBoundingBox BoundingVolume { get; } = new(new Vector3(XLow, Y - AABBPadding, ZLow), new Vector3(XHigh, Y + AABBPadding, ZHigh));

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

		Vector3 centre     = new((XLow + XHigh) / 2f, Y, (ZLow + ZHigh) / 2f); //Average high/low points
		Vector3 localPoint = worldPoint - centre;

		Vector2 uv = new((x - XLow) / (XHigh - XLow), (z - ZLow) / (ZHigh - ZLow));
		Vector3 outwardNormal =
				//See XYPlane.cs for explanation of this
				ray.Origin.Y < Y
						? new Vector3(0, -1, 0)
						: new Vector3(0, 1,  0);
		//Pretend front face is always true, since a 2D plane doesn't really have an 'inside'
		return new HitRecord(ray, worldPoint, localPoint, outwardNormal, k, true, uv);
	}

	/// <inheritdoc />
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