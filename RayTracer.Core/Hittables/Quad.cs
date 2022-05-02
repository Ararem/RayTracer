using RayTracer.Core.Acceleration;
using System.Numerics;
using static System.Numerics.Vector3;

namespace RayTracer.Core.Hittables;

/// <summary>
///  Bounded version of <see cref="InfinitePlane"/>
/// </summary>
/// <param name="A">First corner</param>
/// <param name="B">Second corner, between <paramref name="A"/> and <paramref name="C"/>. This is treated as the 'origin' of the quad (UV's are <see cref="Vector2.Zero"/> here</param>
/// <param name="C">Third corner</param>
/// <remarks>
/// The quad is assumed to be in the shape
/// <code>
/// A --------- +
/// |           |
/// |           |
/// |           |
/// B --------- C
/// </code>
/// </remarks>
//I'm using this answer as reference https://stackoverflow.com/a/21114992
public record Quad(Vector3 A, Vector3 B, Vector3 C) : Hittable
{
	/// <summary>
	///	Normal direction of the quad
	/// </summary>
	public Vector3 Normal { get; } = Normalize(Cross(A -B, C -B)); //Cross of the two side directions, giving a vector perpendicular to both (which is the normal)

	/// <summary>
	/// Vector direction of the side going from <see cref="B"/>==> <see cref="A"/>
	/// </summary>
	public Vector3 SideDirectionBToA { get; } = Normalize(A -B);
	/// <summary>
	/// Vector direction of the side going from <see cref="B"/>==> <see cref="C"/>
	/// </summary>
	public Vector3 SideDirectionBToC { get; } = Normalize(C -B);

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
			// float d = -Dot(B, Normal);
			// t = -(Dot(ray.Origin, Normal) + d) / normDotDir;
			t = -Dot(Normal, ray.Origin - B)   / normDotDir;
		}

		//Assert K ranges
		if ((t < kMin) || (t > kMax)) return null;

		Vector3 worldPoint       = ray.PointAt(t);
		Vector3 localPoint       = worldPoint - B; //Treat B as the origin
		//Project the point onto the edge direction vectors
		float u = Dot(localPoint, SideDirectionBToA),
			v   = Dot(localPoint, SideDirectionBToC);

		//Assert our bounds of the quad (ensure the point is inside)
		if (u is < 0 or > 1 || v is < 0 or > 1) return null;

		Vector2 uv      = new (u,v);
		bool    outside = normDotDir < 0; //True if hit on the same side as the normal points to

		return new HitRecord(ray, worldPoint, localPoint, Normal, t, outside, uv);
	}
}