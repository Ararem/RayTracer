using JetBrains.Annotations;
using RayTracer.Core.Acceleration;
using System.Numerics;
using static System.Numerics.Vector3;

namespace RayTracer.Core.Hittables;

/// <summary>
///  Bounded version of <see cref="InfinitePlane"/>. Created using an origin point, and two vector directions for the sides of the quad (these do not
///  need to be normalized, as their length is used to infer the size of the quad)
/// </summary>
/// <remarks>
///  The quad is assumed to be in the shape
///  <code>
/// O+V ----------+     ^
///   |           |    | |
///   |           |    | |
///   |           |    |V|
///   O --------- O+U  | |
///                    | |
/// =====U=====>       | |
/// </code>
///  The arrows in the above diagram show how the direction vectors U and V are interpreted
/// </remarks>
//I'm using this answer as reference https://stackoverflow.com/a/21114992
[PublicAPI]
public record Quad(Vector3 Origin, Vector3 U, Vector3 V) : Hittable
{
	/// <summary>
	///  Normal direction of the quad
	/// </summary>
	public Vector3 Normal => Normalize(Cross(U, V)); //Cross of the two side directions, giving a vector perpendicular to both (which is the normal)

	/// <inheritdoc/>
	public override AxisAlignedBoundingBox BoundingVolume { get; } = //WARN: Broken
		//AxisAlignedBoundingBox.Encompass(A,B,C);
		AxisAlignedBoundingBox.Infinite;

	/// <inheritdoc/>
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
			t = -Dot(Normal, ray.Origin - Origin) / normDotDir;
		}

		//Assert K ranges
		if ((t < kMin) || (t > kMax)) return null;

		Vector3 worldPoint = ray.PointAt(t);
		Vector3 localPoint = worldPoint - Origin;
		//Project the point onto the edge direction vectors
		float u = Dot(localPoint, U), v = Dot(localPoint, V);
		//Since the side vectors (and the local point) aren't normalized, we have to account for that
		//So Sqrt and divide by the side length
		u = MathF.Sqrt(u / U.LengthSquared());
		v = MathF.Sqrt(v / V.LengthSquared());

		//Assert our bounds of the quad (ensure the point is inside)
		if (u is < 0 or > 1 or float.NaN || v is < 0 or > 1 or float.NaN) return null; //NaN is if value was negative before sqrt

		Vector2 uv      = new(u, v);
		bool    outside = normDotDir < 0; //True if hit on the same side as the normal points to

		return new HitRecord(ray, worldPoint, localPoint, Normal, t, outside, uv);
	}
}