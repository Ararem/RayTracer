using RayTracer.Core;
using RayTracer.Core.Acceleration;
using System.Numerics;
using static System.MathF;
using static System.Numerics.Vector3;

namespace RayTracer.Impl.Hittables;

/// <summary>A cylinder, defined by two points and a radius around the line segments of those points</summary>
public sealed class Cylinder : SingleMaterialHittable
{
	/// <summary>Centre of the cylinder, halfway between <see cref="P1"/> and <see cref="P2"/></summary>
	private readonly Vector3 centre;

	/// <summary><see cref="P2"/> - <see cref="P1"/></summary>
	private readonly Vector3 p2MinusP1;

	/// <summary><see cref="p2MinusP1"/> dotted with itself</summary>
	private readonly float p2MinusP1Dot2;

	/// <summary><see cref="Radius"/>^2 * <see cref="p2MinusP1Dot2"/></summary>
	private readonly float radiusSqrTimesP2P1Dot2;

	/// <summary>A cylinder, defined by two points and a radius around the line segments of those points</summary>
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

		//Cached vars
		p2MinusP1              = P2 - P1;
		p2MinusP1Dot2          = Dot(p2MinusP1, p2MinusP1);
		radiusSqrTimesP2P1Dot2 = Radius * Radius * p2MinusP1Dot2;
	}

	/// <inheritdoc/>
	public override AxisAlignedBoundingBox BoundingVolume { get; }

	/// <summary>The point defining one of the ends of the cylinder</summary>
	public Vector3 P1 { get; }

	/// <summary>The point defining one of the ends of the cylinder</summary>
	public Vector3 P2 { get; }

	/// <summary>The radius of the cylinder</summary>
	public float Radius { get; }

	/// <inheritdoc/>
	public override HitRecord? TryHit(Ray ray, float kMin, float kMax)
	{
		float   k;
		Vector3 normal;
		Vector3 oc = ray.Origin - P1;

		float bard = Dot(p2MinusP1, ray.Direction);
		float baoc = Dot(p2MinusP1, oc);

		float k2 = p2MinusP1Dot2 - (bard * bard);
		float k1 = (p2MinusP1Dot2        * Dot(oc, ray.Direction)) - (baoc * bard);
		float k0 = (p2MinusP1Dot2        * Dot(oc, oc))            - (baoc * baoc) - radiusSqrTimesP2P1Dot2;

		float h = (k1 * k1) - (k2 * k0);
		if (h < 0.0)
		{
			return null;
		}
		else
		{
			h = Sqrt(h);
			k = (-k1 - h) / k2;

			// body
			float y = baoc + (k * bard);
			if ((y > 0.0) && (y < p2MinusP1Dot2))
			{
				normal = ((oc + (k * ray.Direction)) - ((p2MinusP1 * y) / p2MinusP1Dot2)) / Radius;
			}
			// caps
			else
			{
				k = ((y < 0.0f ? 0.0f : p2MinusP1Dot2) - baoc) / bard;
				if (Abs(k1 + (k2 * k)) < h) normal = (p2MinusP1 * Sign(y)) / p2MinusP1Dot2;
				else return null;
			}
		}

		// ReSharper disable once CompareOfFloatsByEqualityOperator
		if ((k == -1f) || !(k >= kMin) || !(k <= kMax))
		{
			return null;
		}
		else
		{
			Vector3 worldPos = ray.PointAt(k);
			Vector3 localPos = worldPos - centre;
			bool    inside   = Dot(ray.Direction, normal) > 0f; //If the ray is 'inside' the sphere

			return new HitRecord(ray, worldPos, localPos, normal, k, !inside, Vector2.Zero, Material); //TODO: UV coords
		}
	}

	/// <inheritdoc/>
	public override bool FastTryHit(Ray ray, float kMin, float kMax)
	{
		float   k;
		Vector3 oc = ray.Origin - P1;

		float bard = Dot(p2MinusP1, ray.Direction);
		float baoc = Dot(p2MinusP1, oc);

		float k2 = p2MinusP1Dot2 - (bard * bard);
		float k1 = (p2MinusP1Dot2        * Dot(oc, ray.Direction)) - (baoc * bard);
		float k0 = (p2MinusP1Dot2        * Dot(oc, oc))            - (baoc * baoc) - radiusSqrTimesP2P1Dot2;

		float h = (k1 * k1) - (k2 * k0);
		if (h < 0.0)
		{
			return false;
		}
		else
		{
			h = Sqrt(h);
			k = (-k1 - h) / k2;

			float y = baoc + (k * bard);
			if (!(y > 0.0) || !(y < p2MinusP1Dot2))
			{
				k = ((y < 0.0f ? 0.0f : p2MinusP1Dot2) - baoc) / bard;
				if (!(Abs(k1 + (k2 * k)) < h))
					return false;
			}
		}

		// ReSharper disable once CompareOfFloatsByEqualityOperator
		return (k != -1f) && (k >= kMin) && (k <= kMax);
	}
}