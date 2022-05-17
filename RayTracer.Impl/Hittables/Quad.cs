using JetBrains.Annotations;
using RayTracer.Core.Acceleration;
using System.Numerics;
using static System.Numerics.Vector3;

namespace RayTracer.Core.Hittables;

/// <summary>
///  Bounded version of <see cref="InfinitePlane"/>. Created using an origin (<see cref="Origin"/>) point, and two vectors (<see cref="U"/>, <see cref="V"/>) for the sides of the quad (these do not
///  need to be normalized, as their length is used to infer the size of the quad). The two side vectors can be non-perpendicular to each other, which creates a parallelogram instead of a rectangle (however they must not be parallel)
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
///    =====U=====>    | |
/// </code>
///  The arrows in the above diagram show how the direction vectors U and V are interpreted.
/// </remarks>
//I'm using this answer as reference https://stackoverflow.com/a/21114992
[PublicAPI]
public record Quad(Vector3 Origin, Vector3 U, Vector3 V) : IHittable
{
	/// <summary>
	/// Creates a new quad from three points, as opposed to a point and two directions
	/// </summary>
	/// <param name="o">Origin of the quad (see <see cref="Origin"/>)</param>
	/// <param name="oPlusU">Position of the point that corresponds to <see cref="Origin"/> + <see cref="U"/>. Used to calculate the <see cref="U"/> vector</param>
	/// <param name="oPlusV">Position of the point that corresponds to <see cref="Origin"/> + <see cref="V"/>. Used to calculate the <see cref="V"/> vector</param>
	/// <returns>A new quad that has corners at the given input points</returns>
	public static Quad CreateFromPoints(Vector3 o, Vector3 oPlusU, Vector3 oPlusV) => new (o, oPlusU - o, oPlusV - o);
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
		//If the ray is going parallel to the plane (along it), then the normal and ray direction will be perpendicular
		if (MathF.Abs(normDotDir) <= 0.001f) //Approx for ==0
		{
			return null;
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

		// Vector3 uvn = Transform(localPoint, LocalToQuadMatrix);
		// float   u   = uvn.X, v = uvn.Y;
		//Inlined code above, since we don't need the Z coord, and removed the casts to `double`
		float   u   = (localPoint.X * LocalToQuadMatrix.M11)  + (localPoint.Y *  LocalToQuadMatrix.M21) + (localPoint.Z *  LocalToQuadMatrix.M31) + LocalToQuadMatrix.M41;
		float   v   =(localPoint.X  *  LocalToQuadMatrix.M12) + (localPoint.Y *  LocalToQuadMatrix.M22) + (localPoint.Z *  LocalToQuadMatrix.M32) + LocalToQuadMatrix.M42;

		//Assert our bounds of the quad (ensure the point is inside)
		if (u is < 0 or > 1 or float.NaN || v is < 0 or > 1 or float.NaN) return null;

		Vector2 uv      = new(u, v);
		bool    outside = normDotDir < 0; //True if hit on the same side as the normal points to

		return new HitRecord(ray, worldPoint, localPoint, Normal, t, outside, uv);
	}

	/// <inheritdoc />
	public override bool FastTryHit(Ray ray, float kMin, float kMax)
	{
		float normDotDir = Dot(Normal, ray.Direction);
		float t;
		//If the ray is going parallel to the plane (along it), then the normal and ray direction will be perpendicular
		if (MathF.Abs(normDotDir) <= 0.001f) //Approx for ==0
		{
			return false;
		}
		else
		{
			//Find intersection normally
			t = -Dot(Normal, ray.Origin - Origin) / normDotDir;
		}

		//Assert K ranges
		if ((t < kMin) || (t > kMax)) return false;

		Vector3 worldPoint = ray.PointAt(t);
		Vector3 localPoint = worldPoint - Origin;

		// Vector3 uvn = Transform(localPoint, LocalToQuadMatrix);
		// float   u   = uvn.X, v = uvn.Y;
		//Inlined code above, since we don't need the Z coord, and removed the casts to `double`
		float u = (localPoint.X * LocalToQuadMatrix.M11)  + (localPoint.Y *  LocalToQuadMatrix.M21) + (localPoint.Z *  LocalToQuadMatrix.M31) + LocalToQuadMatrix.M41;
		float v =(localPoint.X  *  LocalToQuadMatrix.M12) + (localPoint.Y *  LocalToQuadMatrix.M22) + (localPoint.Z *  LocalToQuadMatrix.M32) + LocalToQuadMatrix.M42;

		//Assert our bounds of the quad (ensure the point is inside)
		if (u is < 0 or > 1 or float.NaN || v is < 0 or > 1 or float.NaN) return false;

		return true;
	}
}