using RayTracer.Core;
using RayTracer.Core.Acceleration;
using System.Numerics;

namespace RayTracer.Impl.Hittables;

/// <summary>
///  A plane that spans a region along the XY plane
/// </summary>
/// <param name="XLow">Low X value for this plane</param>
/// <param name="XHigh">High X value for this plane</param>
/// <param name="YLow">Low Y value for this plane</param>
/// <param name="YHigh">High Y value for this plane</param>
/// <param name="Z">Z value the plane is positioned at</param>
/// <param name="AABBPadding">How much to pad the computed AABB by (since the plane is infinitely thin)</param>
public sealed record XYPlane(float XLow, float XHigh, float YLow, float YHigh, float Z, float AABBPadding = 0.001f) : Hittable
{
	/// <inheritdoc/>
	public override AxisAlignedBoundingBox BoundingVolume { get; } = new(new Vector3(XLow, YLow, Z - AABBPadding), new Vector3(XHigh, YHigh, Z + AABBPadding));

	/// <inheritdoc/>
	public override HitRecord? TryHit(Ray ray, float kMin, float kMax)
	{
		//How far along the ray did it intersect with the unbounded version of this plane (x/y bounds of +- infinity)
		float k = (Z - ray.Origin.Z) / ray.Direction.Z;
		//The above code doesn't work when `ray.Direction.Z == 0`, since the ray is essentially going along/parallel to the plane
		//So we have to do a sanity check here, otherwise `k` will be NaN, and that messes up everything else
		if ((ray.Direction.Z == 0f) || float.IsNaN(k)) return null;
		if ((k < kMin) || (k > kMax)) //Out of range for our near/far plane
			return null;

		Vector3 worldPoint = ray.PointAt(k);
		float   x          = worldPoint.X, y = worldPoint.Y;
		//Assert our X/Y bounds
		if ((x < XLow) || (x > XHigh) || (y < YLow) || (y > YHigh))
			return null;

		//Calculate the local point (from the centre of the plane)
		Vector3 centre     = new((XLow + XHigh) / 2f, (YLow + YHigh) / 2f, Z); //Average high/low points
		Vector3 localPoint = worldPoint - centre;

		Vector2 uv = new((x - XLow) / (XHigh - XLow), (y - YLow) / (YHigh - YLow));
		Vector3 outwardNormal =
				/*
				 * This just ensures that the normal points against the ray, as usual
				 *
				 * Explanation:
				 * Lets take the example below, with the ray starting at Z=0, towards +ve Z, and the plane is at Z=1
				 *  		=========>>	[==]	  P
				 *   R		=========>>	[==]	  L
				 *   A		=========>>	[==]	  A
				 *   Y		=========>>	[==]	  N
				 * (Z=0)	=========>>	[==]	  E
				 *			=========>>	[==]	(Z=1)
				 * Here, Ray.Z is smaller than Plane.Z, and the normal should be facing away from the ray (-ve Z)
				 *
				 * Lets take the example below, with the ray starting at Z=1, towards -ve Z, and the plane is at Z=0
				 *   P		[==]	<<=========
				 *   L		[==]	<<=========	  R
				 *   A		[==]	<<=========	  A
				 *   N		[==]	<<=========	  Y
				 *   E		[==]	<<=========	(Z=1)
				 * (Z=0)	[==]	<<=========
				 * Here, Plane.Z is smaller than Ray.Z, and the normal should be facing away from the ray (+ve Z)
				 *
				 * Therefore, if Ray.Z < Plane.Z, then Normal.Z =-1, else Normal.Z = +1
				 */
				ray.Origin.Z < Z
						? new Vector3(0, 0, -1)
						: new Vector3(0, 0, 1);

		//Pretend front face is always true, since a 2D plane doesn't really have an 'inside'
		return new HitRecord(ray, worldPoint, localPoint, outwardNormal, k, true, uv);
	}

	/// <inheritdoc/>
	public override bool FastTryHit(Ray ray, float kMin, float kMax)
	{
		//How far along the ray did it intersect with the unbounded version of this plane (x/y bounds of +- infinity)
		float k = (Z - ray.Origin.Z) / ray.Direction.Z;
		//The above code doesn't work when `ray.Direction.Z == 0`, since the ray is essentially going along/parallel to the plane
		//So we have to do a sanity check here, otherwise `k` will be NaN, and that messes up everything else
		if ((ray.Direction.Z == 0f) || float.IsNaN(k)) return false;
		if ((k < kMin) || (k > kMax)) //Out of range for our near/far plane
			return false;

		Vector3 worldPoint = ray.PointAt(k);
		float   x          = worldPoint.X, y = worldPoint.Y;
		//Assert our X/Y bounds
		if ((x < XLow) || (x > XHigh) || (y < YLow) || (y > YHigh))
			return false;

		return true;
	}
}