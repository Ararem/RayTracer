using RayTracer.Core;
using RayTracer.Core.Acceleration;
using System.Numerics;
using static System.Numerics.Vector3;
using static System.MathF;

namespace RayTracer.Impl.Hittables;

/// <summary>A 2D Disk in 3D space. Defined by a point (the centre of the disk) and a normal direction)</summary>
public sealed class Disk : Hittable
{
	private readonly float radiusSqr;

	/// <summary>A 2D Disk in 3D space. Defined by a point (the centre of the disk) and a normal direction)</summary>
	/// <param name="centre">The centre of the disk in 3D space</param>
	/// <param name="normal">Normal direction of the disk</param>
	/// <param name="radius">How large the radius of the disk is</param>
	public Disk(Vector3 centre, Vector3 normal, float radius)
	{
		Centre           = centre;
		Normal           = normal;
		Radius           = radius;
		radiusSqr        = radius * radius;
		BoundingVolume   = new AxisAlignedBoundingBox(centre - new Vector3(radius), centre + new Vector3(radius));
		negCentreDotNorm = -Dot(Centre, Normal);
	}

	//TODO: Very inefficient, need to make AABB smaller and more compact
	/// <inheritdoc/>
	public override AxisAlignedBoundingBox BoundingVolume { get; }

	/// <summary>The centre of the disk in 3D space</summary>
	public Vector3 Centre { get; }

	/// <summary>Normal direction of the disk</summary>
	public Vector3 Normal { get; }

	/// <summary>How large the radius of the disk is</summary>
	public float Radius { get; }

	/// <summary>
	///-Dot(<see cref="Centre"/>, <see cref="Normal"/>)
	/// </summary>
	private readonly float negCentreDotNorm;

	/// <inheritdoc/>
	//TODO: Disk UV's
	public override HitRecord? TryHit(Ray ray, float kMin, float kMax)
	{
		//Code copied from `Plane.cs`, with a distance checker added
		//https://www.cs.princeton.edu/courses/archive/fall00/cs426/lectures/raycast/sld017.htm
		float normDotDir = Dot(ray.Direction, Normal);
		float t;
		//If the ray is going parallel to the plane (through it), then the normal and ray direction will be perpendicular
		if (Abs(normDotDir) <= 0.001f) //Approx for ==0
		{
			return null;
		}
		else
		{
			//Find intersection normally
			t = -(Dot(ray.Origin, Normal) + negCentreDotNorm) / normDotDir;
		}

		//Assert ranges
		if ((t < kMin) || (t > kMax)) return null;

		Vector3 worldPoint = ray.PointAt(t);

		//Now assert radius
		if (DistanceSquared(Centre, worldPoint) > radiusSqr) return null;

		Vector3 localPoint = worldPoint - Centre;
		bool    outside    = normDotDir < 0; //True if hit on the same side as the normal points to
		Vector2 uv         = Vector2.Zero;   //A problem with uv's is that we can't really map them onto planes which are infinite

		return new HitRecord(ray, worldPoint, localPoint, Normal, t, outside, uv);
	}

	/// <inheritdoc/>
	public override bool FastTryHit(Ray ray, float kMin, float kMax)
	{
		float normDotDir = Dot(ray.Direction, Normal);
		float t;
		//If the ray is going parallel to the plane (through it), then the normal and ray direction will be perpendicular
		if (Abs(normDotDir) <= 0.001f) //Approx for ==0
		{
			return false;
		}
		else
		{
			//Find intersection normally
			t = -(Dot(ray.Origin, Normal) + negCentreDotNorm) / normDotDir;
		}

		//Assert ranges
		if ((t < kMin) || (t > kMax)) return false;

		Vector3 worldPoint = ray.PointAt(t);

		//Now assert radius
		return DistanceSquared(Centre, worldPoint) <= radiusSqr;
	}
}