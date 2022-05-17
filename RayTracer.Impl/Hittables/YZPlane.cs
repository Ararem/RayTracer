using RayTracer.Core.Acceleration;
using System.Numerics;

namespace RayTracer.Core.Hittables;

/// <summary>
///  A plane that spans a region along the XY plane
/// </summary>
/// <param name="YLow">Low Y value for this plane</param>
/// <param name="YHigh">High Y value for this plane</param>
/// <param name="ZLow">Low Z value for this plane</param>
/// <param name="ZHigh">High Z value for this plane</param>
/// <param name="X">X value the plane is positioned at</param>
/// <param name="AABBPadding">How much to pad the computed AABB by (since the plane is infinitely thin)</param>
public sealed record YZPlane(float YLow, float YHigh, float ZLow, float ZHigh, float X, float AABBPadding = 0.001f) : IHittable
{
	/// <inheritdoc/>
	public override AxisAlignedBoundingBox BoundingVolume { get; } = new(new Vector3(X - AABBPadding, YLow, ZLow), new Vector3(X + AABBPadding, YHigh, ZHigh));

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
						? new Vector3(-1, 0, 0)
						: new Vector3(1,  0, 0);
		//Pretend front face is always true, since a 2D plane doesn't really have an 'inside'
		return new HitRecord(ray, worldPoint, localPoint, outwardNormal, k, true, uv);
	}

	/// <inheritdoc />
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