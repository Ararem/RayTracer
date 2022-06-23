using RayTracer.Core;
using RayTracer.Core.Acceleration;
using System.Numerics;

namespace RayTracer.Impl.Hittables;

/// <summary>A plane that spans a region along the XY plane</summary>
public sealed class XYPlane : SingleMaterialHittable
{
	private readonly Vector3 centre;

	/// <summary>Creates a new plane from the specified corner points</summary>
	/// <param name="xLow">Low X value for this plane</param>
	/// <param name="xHigh">High X value for this plane</param>
	/// <param name="yLow">Low Y value for this plane</param>
	/// <param name="yHigh">High Y value for this plane</param>
	/// <param name="z">Z value the plane is positioned at</param>
	/// <param name="aabbPadding">How much to pad the computed AABB by (since the plane is infinitely thin)</param>
	public XYPlane(float xLow, float xHigh, float yLow, float yHigh, float z, float aabbPadding = 0.001f)
	{
		XLow           = xLow;
		XHigh          = xHigh;
		YLow           = yLow;
		YHigh          = yHigh;
		Z              = z;
		AABBPadding    = aabbPadding;
		BoundingVolume = new AxisAlignedBoundingBox(new Vector3(xLow, yLow, z - aabbPadding), new Vector3(xHigh, yHigh, z + aabbPadding));
		centre         = new Vector3((XLow + XHigh) / 2f, (YLow + YHigh) / 2f, Z);
	}

	/// <inheritdoc/>
	public override AxisAlignedBoundingBox BoundingVolume { get; }

	/// <summary>Low X value for this plane</summary>
	public float XLow { get; }

	/// <summary>High X value for this plane</summary>
	public float XHigh { get; }

	/// <summary>Low Y value for this plane</summary>
	public float YLow { get; }

	/// <summary>High Y value for this plane</summary>
	public float YHigh { get; }

	/// <summary>Z value the plane is positioned at</summary>
	public float Z { get; }

	/// <summary>How much to pad the computed AABB by (since the plane is infinitely thin)</summary>
	public float AABBPadding { get; }

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
						? -Vector3.UnitZ
						: Vector3.UnitZ;

		//Pretend front face is always true, since a 2D plane doesn't really have an 'inside'
		return new HitRecord(ray, worldPoint, localPoint, outwardNormal, k, true, uv,this, Material);
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