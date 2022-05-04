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
	private Matrix4x4 LocalToQuadMatrix { get; } = GetLocalToQuadMatrix(U, V);

	/// <summary>
	///  Normal direction of the quad
	/// </summary>
	public Vector3 Normal { get; } = Normalize(Cross(U, V)); //Cross of the two side directions, giving a vector perpendicular to both (which is the normal)

	/// <inheritdoc/>
	public override AxisAlignedBoundingBox BoundingVolume { get; } = //WARN: Broken
		//AxisAlignedBoundingBox.Encompass(A,B,C);
		AxisAlignedBoundingBox.Infinite;

	private static Matrix4x4 GetLocalToQuadMatrix(Vector3 u, Vector3 v)
	{
		//This UV/World space conversion code is essentially just transforming points between two different coordinate systems
		//So from World Coords (X,Y,Z) ==> Quad (UV) Coords (U,V,N)
		//First step we need to get the matrix that converts Quad coords => World Coords
		//This is easy, since it's essentially just our UV vectors
		//If we have a vector that's the length and direction of our U side, it's (1, 0, 0) in quad coords
		//In world coords it's just the U vector
		//This StackOverflow answer was helpful in creating the matrix: https://stackoverflow.com/questions/31257325/converting-points-into-another-coordinate-system

		Vector3 n = Normalize(Cross(u, v));
        Matrix4x4 quadToWorld = new(
          u.X, u.Y, u.Z, 0,
          v.X, v.Y, v.Z, 0,
          n.X, n.Y, n.Z, 0,
          0, 0, 0, 1
        );

        if (!Matrix4x4.Invert(quadToWorld, out Matrix4x4 worldToQuad)) throw new ArithmeticException("Could not invert Quad to world transformation matrix (UV coords were probably parallel)");

        return worldToQuad;
    }

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

		Vector3 uvn = Transform(localPoint, LocalToQuadMatrix);
		float   u   = uvn.X, v = uvn.Y;

		//Assert our bounds of the quad (ensure the point is inside)
		if (u is < 0 or > 1 or float.NaN || v is < 0 or > 1 or float.NaN) return null;

		Vector2 uv      = new(u, v);
		bool    outside = normDotDir < 0; //True if hit on the same side as the normal points to

		return new HitRecord(ray, worldPoint, localPoint, Normal, t, outside, uv);
	}
}