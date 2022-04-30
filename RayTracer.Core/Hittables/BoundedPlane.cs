using RayTracer.Core.Acceleration;
using System.Numerics;
using static System.Numerics.Vector3;

namespace RayTracer.Core.Hittables;

/// <summary>
///  Bounded version of <see cref="InfinitePlane"/>
/// </summary>
public record BoundedPlane(Vector3 Point, Vector3 Normal, float BoundU, float BoundV) : Hittable
{
	public Vector3 U { get; } = CalculateUvVectors(Normal).U;
	public Vector3 V { get; } = CalculateUvVectors(Normal).V;

	/// <summary>
	/// Creates two vectors such that they are both perpendicular to themselves and the <paramref name="normal"/>. Both will lie in the plane that has a normal specified by <paramref name="normal"/>
	/// </summary>
	/// <param name="normal">Normal of the plane</param>
	public static (Vector3 U, Vector3 V) CalculateUvVectors(Vector3 normal)
	{
		//Thanks to an anonymous user for this answer https://stackoverflow.com/a/18664291

		/*
		 * Here we try find a vector that's perpendicular to the plane, by crossing a random vector with the normal
		 * (This outputs a vector that's perpendicular to both, so it's gonna be along the plane)
		 * If the `rand` vector is parallel to the normal, then this won't work and the `Cross` will return the Zero vector
		 * The `UnitX` and `UnitY` vectors are semi-arbitrary, I'm just picking two non-parallel vectors
		*/
		Vector3 v1         = Cross(normal, UnitY);
		if (v1 == Zero) v1 = Cross(normal, UnitY);//Fallback if N ∥ UnitX (colinear)

		v1 = Normalize(v1);
		Vector3 v2 = Normalize(Cross(normal, v1)); //Find a second orthogonal vector from our first

		return (v1, v2);
	}

	/// <inheritdoc />
	public override AxisAlignedBoundingBox BoundingVolume { get; } = ?????;

	/// <inheritdoc />
	public override HitRecord? TryHit(Ray ray, float kMin, float kMax)
	{
		//https://www.cs.princeton.edu/courses/archive/fall00/cs426/lectures/raycast/sld017.htm
		float normDotDir = Dot(ray.Direction, Normal);
		float t;
		//If the ray is going parallel to the plane (through it), then the normal and ray direction will be perpendicular
		if (MathF.Abs(normDotDir) <= 0.001f) //Approx for ==0
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
		Vector3 localPoint = worldPoint - Point;
		//Project the points
		/*
		 * This
		 * [x, y, z] =
		 */
		Vector2 uv         = Vector2.Zero;   //A problem with uv's is that we can't really map them onto planes which are infinite

		bool outside = normDotDir < 0; //True if hit on the same side as the normal points to

		return new HitRecord(ray, worldPoint, localPoint, Normal, t, outside, uv);
	}
}