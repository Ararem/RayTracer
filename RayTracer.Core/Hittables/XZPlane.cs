using RayTracer.Core.Graphics;
using System.Numerics;

namespace RayTracer.Core.Hittables;

using static MathF;

public record XZPlane : Hittable
{
	private readonly Vector3 centre;

	public XZPlane(float xLow, float xHigh, float zLow, float zHigh, float y, float aabbPadding = 0.001f)
	{
		if (aabbPadding <= 0)
			throw new ArgumentOutOfRangeException(nameof(aabbPadding), aabbPadding, "Padding for the AABB must be > 0");
		XLow        = Min(xLow, xHigh);
		XHigh       = Max(xLow, xHigh);
		ZLow        = Min(zLow, zHigh);
		ZHigh       = Max(zLow, zHigh);
		Y           = y;
		AABBPadding = aabbPadding;
		centre      = new Vector3((XLow + XHigh) / 2f, Y, (ZLow + ZHigh) / 2f); //Average high/low points
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
	///  Low Z value for this plane
	/// </summary>
	public float ZLow { get; }

	/// <summary>
	///  High Z value for this plane
	/// </summary>
	public float ZHigh { get; }

	/// <summary>
	///  Y value for this plane
	/// </summary>
	public float Y { get; }

	/// <summary>
	///  How much to pad the computed AABB by (since the plane is infinitely thin)
	/// </summary>
	public float AABBPadding { get; }

	/// <inheritdoc/>
	public override HitRecord? TryHit(Ray ray, float kMin, float kMax)
	{
		//How far along the ray did it intersect with the unbounded version of this plane (bounds of +- infinity)
		float k = (Y - ray.Origin.Y) / ray.Direction.Y;
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
						? new Vector3(0, -1, 0)
						: new Vector3(0, 1,  0);
		//Pretend front face is always true, since a 2D plane doesn't really have an 'inside'
		return new HitRecord(ray, worldPoint, localPoint, outwardNormal, k, true, uv);
	}
}