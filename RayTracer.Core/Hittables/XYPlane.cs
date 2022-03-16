using RayTracer.Core.Graphics;
using System.Numerics;

namespace RayTracer.Core.Hittables;

using static MathF;

public class XYPlane : Hittable
{
	private readonly Vector3 centre;

	public XYPlane(float xLow, float xHigh, float yLow, float yHigh, float z, float aabbPadding = 0.001f)
	{
		if (aabbPadding <= 0)
			throw new ArgumentOutOfRangeException(nameof(aabbPadding), aabbPadding, "Padding for the AABB must be > 0");
		XLow        = Min(xLow, xHigh);
		XHigh       = Max(xLow, xHigh);
		YLow        = Min(yLow, yHigh);
		YHigh       = Max(yLow, yHigh);
		Z           = z;
		AABBPadding = aabbPadding;
		centre      = new Vector3((XLow + XHigh) / 2f, (YLow + YHigh) / 2f, Z); //Average high/low points
	}

	/// <summary>
	///  Low X value for this plane
	/// </summary>
	public float XLow { get; }

	/// <summary>
	///  High X value for this plane
	/// </summary>
	public float XHigh { get; }

	/// <summary>
	///  Low Y value for this plane
	/// </summary>
	public float YLow { get; }

	/// <summary>
	///  High Y value for this plane
	/// </summary>
	public float YHigh { get; }

	/// <summary>
	///  Z value for this plane
	/// </summary>
	public float Z { get; }

	/// <summary>
	///  How much to pad the computed AABB by (since the plane is infinitely thin)
	/// </summary>
	public float AABBPadding { get; }

	/// <inheritdoc/>
	public override HitRecord? TryHit(Ray ray, float kMin, float kMax)
	{
		//How far along the ray did it intersect with the unbounded version of this plane (x/y bounds of +- infinity)
		float k = (Z - ray.Origin.Z) / ray.Direction.Z;
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
						? new Vector3(0, 0, -1)
						: new Vector3(0, 0, 1);

		//Pretend front face is always true, since a 2D plane doesn't really have an 'inside'
		return new HitRecord(ray, worldPoint, localPoint, outwardNormal, k, true, uv);
	}
}