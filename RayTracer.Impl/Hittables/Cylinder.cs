using RayTracer.Core;
using RayTracer.Core.Acceleration;
using System.Numerics;
using static System.MathF;
using static System.Numerics.Vector3;

namespace RayTracer.Impl.Hittables;

/// <summary>
///  A cylinder, defined by two points and a radius around the line segments of those points
/// </summary>
public class Cylinder : Hittable
{
	/// <summary>
	///  A cylinder, defined by two points and a radius around the line segments of those points
	/// </summary>
	/// <param name="p1">The point defining one of the ends of the cylinder</param>
	/// <param name="p2">The point defining one of the ends of the cylinder</param>
	/// <param name="radius">The radius of the cylinder</param>
	public Cylinder(Vector3 p1, Vector3 p2, float radius)
	{
		P1             = p1;
		P2             = p2;
		Radius         = radius;
		centre         = Lerp(p1, p2, 0.5f);
		BoundingVolume = new AxisAlignedBoundingBox(Min(p1, p2) - new Vector3(radius), Max(p1, p2) + new Vector3(radius));
	}

	/// <inheritdoc/>
	public override AxisAlignedBoundingBox BoundingVolume { get; }

	/// <summary>The point defining one of the ends of the cylinder</summary>
	public Vector3 P1 { get; }

	/// <summary>The point defining one of the ends of the cylinder</summary>
	public Vector3 P2 { get; }

	/// <summary>The radius of the cylinder</summary>
	public float Radius { get; }

	private readonly Vector3 centre; //Halfway between P1 and P2

	/// <inheritdoc/>
	public override HitRecord? TryHit(Ray ray, float kMin, float kMax)
	{
		//XYZ are XYZ of normal, W is k value along ray of intersection
		Vector4 kNor;

		Vector3 ba = P2         - P1;
		Vector3 oc = ray.Origin - P1;

		float baba = Dot(ba, ba);
		float bard = Dot(ba, ray.Direction);
		float baoc = Dot(ba, oc);

		float k2 = baba - (bard * bard);
		float k1 = (baba        * Dot(oc, ray.Direction)) - (baoc * bard);
		float k0 = (baba        * Dot(oc, oc))            - (baoc * baoc) - (Radius * Radius * baba);

		float h = (k1 * k1) - (k2 * k0);
		if (h < 0.0)
		{
			return null;
		}
		else
		{
			h = Sqrt(h);
			float t = (-k1 - h) / k2;

			// body
			float y = baoc + (t * bard);
			if ((y > 0.0) && (y < baba))
			{
				kNor = new Vector4(((oc + (t * ray.Direction)) - ((ba * y) / baba)) / Radius, t);
			}
			// caps
			else
			{
				t = ((y < 0.0f ? 0.0f : baba) - baoc) / bard;
				if (Abs(k1 + (k2 * t)) < h) kNor = new Vector4((ba * Sign(y)) / baba, t);
				else return null;
			}
		}

		float k = kNor.W;
		// ReSharper disable once CompareOfFloatsByEqualityOperator
		if ((k != -1f) && (k >= kMin) && (k <= kMax))
		{
			Vector3 normal   = new(kNor.X, kNor.Y, kNor.Z);
			Vector3 worldPos = ray.PointAt(k);
			Vector3 localPos = worldPos - centre;
			bool    inside   = Dot(ray.Direction, normal) > 0f; //If the ray is 'inside' the sphere

			return new HitRecord(ray, worldPos, localPos, normal, k, !inside, Vector2.Zero); //TODO: UV coords
		}
		else
		{
			return null;
		}
	}

	/// <inheritdoc/>
	public override bool FastTryHit(Ray ray, float kMin, float kMax)
	{
		//XYZ are XYZ of normal, W is k value along ray of intersection
		Vector4 kNor;

		Vector3 ba = P2         - P1;
		Vector3 oc = ray.Origin - P1;

		float baba = Dot(ba, ba);
		float bard = Dot(ba, ray.Direction);
		float baoc = Dot(ba, oc);

		float k2 = baba - (bard * bard);
		float k1 = (baba        * Dot(oc, ray.Direction)) - (baoc * bard);
		float k0 = (baba        * Dot(oc, oc))            - (baoc * baoc) - (Radius * Radius * baba);

		float h = (k1 * k1) - (k2 * k0);
		if (h < 0.0)
		{
			return false;
		}
		else
		{
			h = Sqrt(h);
			float t = (-k1 - h) / k2;

			// body
			float y = baoc + (t * bard);
			if ((y > 0.0) && (y < baba))
			{
				kNor = new Vector4(((oc + (t * ray.Direction)) - ((ba * y) / baba)) / Radius, t);
			}
			// caps
			else
			{
				t = ((y < 0.0f ? 0.0f : baba) - baoc) / bard;
				if (Abs(k1 + (k2 * t)) < h) kNor = new Vector4((ba * Sign(y)) / baba, t);
				else return false;
			}
		}

		float k = kNor.W;
		// ReSharper disable once CompareOfFloatsByEqualityOperator
		return (k != -1f) && (k >= kMin) && (k <= kMax);
	}
}