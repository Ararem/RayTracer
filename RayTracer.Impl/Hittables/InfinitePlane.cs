using RayTracer.Core;
using RayTracer.Core.Acceleration;
using System.Numerics;
using static System.Numerics.Vector3;
using static System.MathF;

namespace RayTracer.Impl.Hittables;

/// <summary>
///  A 2D Plane in 3D space.
/// </summary>
/// <remarks>
///  Due to being 'infinite', UV coordinates for the <see cref="HitRecord"/> do not exist, and so are assigned <see cref="Vector2.Zero"/>
/// </remarks>
public sealed class InfinitePlane : Hittable
{
	/// <summary>
	///  A 2D Plane in 3D space.
	/// </summary>
	/// <param name="point">A point on the plane</param>
	/// <param name="normal">Normal of the plane</param>
	public InfinitePlane(Vector3 point, Vector3 normal)
	{
		Point  = point;
		Normal = normal;
	}

	/// <inheritdoc/>
	public override AxisAlignedBoundingBox BoundingVolume => AxisAlignedBoundingBox.Infinite;

	/// <summary>A point on the plane</summary>
	public Vector3 Point { get; }

	/// <summary>Normal of the plane</summary>
	public Vector3 Normal { get; }

	/// <inheritdoc/>
	public override HitRecord? TryHit(Ray ray, float kMin, float kMax)
	{
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
			float d = -Dot(Point, Normal);
			t = -(Dot(ray.Origin, Normal) + d) / normDotDir;
		}

		//Assert ranges
		if ((t < kMin) || (t > kMax)) return null;

		Vector3 worldPoint = ray.PointAt(t);
		Vector3 localPoint = worldPoint - Point;
		bool    outside    = normDotDir < 0; //True if hit on the same side as the normal points to
		Vector2 uv         = Vector2.Zero;   //A problem with uv's is that we can't really map them onto planes which are infinite

		return new HitRecord(ray, worldPoint, localPoint, Normal, t, outside, uv);
	}

	/// <inheritdoc/>
	public override bool FastTryHit(Ray ray, float kMin, float kMax)
	{
		//https://www.cs.princeton.edu/courses/archive/fall00/cs426/lectures/raycast/sld017.htm
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
			float d = -Dot(Point, Normal);
			t = -(Dot(ray.Origin, Normal) + d) / normDotDir;
		}

		//Assert ranges
		return (t >= kMin) && (t <= kMax);
	}
}