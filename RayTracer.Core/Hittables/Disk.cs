using RayTracer.Core.Acceleration;
using System.Numerics;
using static System.Numerics.Vector3;
using static System.MathF;

namespace RayTracer.Core.Hittables;

/// <summary>
///  A 2D Disk in 3D space. Defined by a point (the centre of the disk) and a normal direction)
/// </summary>
/// <param name="Centre">The centre of the disk in 3D space</param>
/// <param name="Normal">Normal direction of the disk</param>
/// <param name="Radius">How large the radius of the disk is</param>
public sealed record Disk(Vector3 Centre, Vector3 Normal, float Radius) : Hittable
{
	private readonly float radiusSqr = Radius * Radius;

	//TODO: Very inefficient, need to make AABB smaller and more compact
	/// <inheritdoc/>
	public override AxisAlignedBoundingBox BoundingVolume { get; } = new(Centre - new Vector3(Radius), Centre + new Vector3(Radius));

	/// <inheritdoc/>
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
			float d = -Dot(Centre, Normal);
			t = -(Dot(ray.Origin, Normal) + d) / normDotDir;
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
}