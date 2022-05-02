using RayTracer.Core.Acceleration;
using System.Numerics;
using static System.Numerics.Vector3;

namespace RayTracer.Core.Hittables;

/// <summary>
///  Bounded version of <see cref="InfinitePlane"/>
/// </summary>
/// <remarks>
/// The quad is assumed to be in the shape
/// A --------- +
/// |			|
/// |			|
/// |			|
/// B --------- C
/// </remarks>
//I'm using this answer as reference https://stackoverflow.com/a/21114992
public record Quad(Vector3 A, Vector3 B, Vector3 C) : Hittable
{
	public Vector3 Normal { get; } = Normalize(Cross(B - A, B - C));

	public Vector3 SideDirBA { get; } = Normalize(B - A);
	public Vector3 SideDirBC { get; } = Normalize(B - C);

	/// <inheritdoc />
	public override AxisAlignedBoundingBox BoundingVolume { get; } = //WARN: Broken
		//AxisAlignedBoundingBox.Encompass(A,B,C);
		AxisAlignedBoundingBox.Infinite;

	/// <inheritdoc />
	public override HitRecord? TryHit(Ray ray, float kMin, float kMax)
	{
		float normDotDir = Dot(Normal, ray.Direction);
		float t;
		//If the ray is going parallel to the plane (through it), then the normal and ray direction will be perpendicular
		if (MathF.Abs(normDotDir) <= 0.001f) //Approx for ==0
		{
			return null;
			t = kMin; //Going inside plane, find lowest point along the ray
		}
		else
		{
			//Find intersection normally
			// float d = -Dot(A, Normal);
			// t = -(Dot(ray.Origin, Normal) + d) / normDotDir;
			t = -Dot(Normal, ray.Origin - B)   / normDotDir;
		}

		//Assert K ranges
		if ((t < kMin) || (t > kMax)) return null;

		Vector3 worldPoint       = ray.PointAt(t);
		Vector3 localPoint       = worldPoint - A; //Treat A as the origin
		//Project the point onto the edge direction vectors
		float u = Dot(localPoint, SideDirBA)/SideDirBA.Length(),
			v   = Dot(localPoint, SideDirBC)/SideDirBC.Length();

		//Assert our bounds of the quad (ensure the point is inside)
		if (u is < 0 or > 1 || v is < 0 or > 1) return null;

		Vector2 uv      = new (u,v);
		bool    outside = normDotDir < 0; //True if hit on the same side as the normal points to

		return new HitRecord(ray, worldPoint, localPoint, Normal, t, outside, uv);
	}
}