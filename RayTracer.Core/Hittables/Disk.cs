using RayTracer.Core.Graphics;
using System.Numerics;
using static System.Numerics.Vector3;
using static System.MathF;

namespace RayTracer.Core.Hittables;

//TODO: Constraints/Bounds for planes
public sealed record Disk(Vector3 Point, Vector3 Normal, float Radius) : Hittable
{
	private Lazy<float> radiusSqr = new(() => Radius * Radius);
	/// <inheritdoc />
	public override HitRecord? TryHit(Ray ray, float kMin, float kMax)
	{
		//Code copied from `Plane.cs`, with a distance checker added
		//https://www.cs.princeton.edu/courses/archive/fall00/cs426/lectures/raycast/sld017.htm
		float normDotDir = Dot(ray.Direction, Normal);
		float t;
		//If the ray is going parallel to the plane (through it), then the normal and ray direction will be perpendicular
		if (Abs(normDotDir) <= 0.001f) //Approx for ==0
		{
			t = kMin; //Going inside plane, find lowest point along the ray
		}
		else
		{
			//Find intersection normally
			float d = -Dot(Point, Normal);
			t = -(Dot(ray.Origin, Normal) + d) / normDotDir;
		}

		//Assert ranges
		if ((t < kMin) || (t > kMax)) return null;

		Vector3 worldPoint = ray.PointAt(t);

		//Now assert radius
		if (DistanceSquared(Point, worldPoint) > radiusSqr.Value) return null;

		Vector3 localPoint = worldPoint - Point;
		bool    outside    = normDotDir < 0; //True if hit on the same side as the normal points to
		Vector2 uv         = Vector2.Zero;   //A problem with uv's is that we can't really map them onto planes which are infinite

		return new HitRecord(ray, worldPoint, localPoint, Normal, t, outside, uv);
	}
}