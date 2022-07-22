using Ararem.RayTracer.Core;
using Ararem.RayTracer.Core.Acceleration;
using JetBrains.Annotations;
using System.Numerics;
using static System.Numerics.Vector3;

namespace Ararem.RayTracer.Impl.Hittables;

/// <summary>
///  Bounded version of <see cref="InfinitePlane"/>. Created using an origin (<see cref="Origin"/>) point, and two vectors (<see cref="U"/>,
///  <see cref="V"/>) for the sides of the quad (these do not need to be normalized, as their length is used to infer the size of the quad). The two side
///  vectors can be non-perpendicular to each other, which creates a parallelogram instead of a rectangle (however they must not be parallel)
/// </summary>
//I'm using this answer as reference https://stackoverflow.com/a/21114992
public sealed class Quad : SingleMaterialHittable
{
	private readonly Matrix4x4 localToQuadMatrix;

	/// <summary>
	///  Creates a new quad using an origin (<see cref="Origin"/>) point, and two vectors (<see cref="U"/>, <see cref="V"/>) for the sides of the quad (these
	///  do not need to be normalized, as their length is used to infer the size of the quad). The two side vectors can be non-perpendicular to each other,
	///  which creates a parallelogram instead of a rectangle (however they must not be parallel)
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
	public Quad(Vector3 origin, Vector3 u, Vector3 v)
	{
		Origin         = origin;
		U              = u;
		V              = v;
		Normal         = Normalize(Cross(u, v));
		BoundingVolume = AxisAlignedBoundingBox.Encompass(Origin, Origin + U, Origin + V, Origin + U + V).WithPadding(.0001f);

		//This UV/World space conversion code is essentially just transforming points between two different coordinate systems
		//So from World Coords (X,Y,Z) ==> Quad (UV) Coords (U,V,N)
		//First step we need to get the matrix that converts Quad coords => World Coords
		//This is easy, since it's essentially just our UV vectors
		//If we have a vector that's the length and direction of our U side, it's (1, 0, 0) in quad coords
		//In world coords it's just the U vector
		//This StackOverflow answer was helpful in creating the matrix: https://stackoverflow.com/questions/31257325/converting-points-into-another-coordinate-system
		Matrix4x4 quadToWorld = new(
				u.X, u.Y, u.Z, 0,
				v.X, v.Y, v.Z, 0,
				Normal.X, Normal.Y, Normal.Z, 0,
				0, 0, 0, 1
		);

		if (!Matrix4x4.Invert(quadToWorld, out localToQuadMatrix)) throw new ArithmeticException("Could not invert Quad to world transformation matrix (UV coords were probably parallel)");
	}

	/// <summary>Normal direction of the quad</summary>
	public Vector3 Normal { get; } //Cross of the two side directions, giving a vector perpendicular to both (which is the normal)

	/// <inheritdoc/>
	public override AxisAlignedBoundingBox BoundingVolume { get; }

	/// <summary>The 'origin' point of the quad - the <see cref="U"/> and <see cref="V"/> vectors are treated as originating from this point</summary>
	public Vector3 Origin { get; }

	/// <summary>The side vector of the first side of the quad</summary>
	[PublicAPI]
	public Vector3 U { get; }

	/// <summary>The side vector of the second side of the quad</summary>
	[PublicAPI]
	public Vector3 V { get; }

	/// <summary>Creates a new quad from three points, as opposed to a point and two directions</summary>
	/// <param name="o">Origin of the quad (see <see cref="Origin"/>)</param>
	/// <param name="oPlusU">Position of the point that corresponds to <see cref="Origin"/> + <see cref="U"/>. Used to calculate the <see cref="U"/> vector</param>
	/// <param name="oPlusV">Position of the point that corresponds to <see cref="Origin"/> + <see cref="V"/>. Used to calculate the <see cref="V"/> vector</param>
	/// <returns>A new quad that has corners at the given input points</returns>
	public static Quad CreateFromPoints(Vector3 o, Vector3 oPlusU, Vector3 oPlusV) => new(o, oPlusU - o, oPlusV - o);

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
			//Note to future me: DON'T OPTIMISE LIKE WITH OTHER PLANAR SHAPES, IT DOESN'T WORK
			t = -Dot(Normal, ray.Origin - Origin) / normDotDir;
		}

		//Assert K ranges
		if ((t < kMin) || (t > kMax)) return null;

		Vector3 worldPoint = ray.PointAt(t);
		Vector3 localPoint = worldPoint - Origin;

		// Vector3 uvn = Transform(localPoint, LocalToQuadMatrix);
		// float   u   = uvn.X, v = uvn.Y;
		//Inlined code above, since we don't need the Z coord, and removed the casts to `double`
		float u = (localPoint.X * localToQuadMatrix.M11) + (localPoint.Y * localToQuadMatrix.M21) + (localPoint.Z * localToQuadMatrix.M31) + localToQuadMatrix.M41;
		float v = (localPoint.X * localToQuadMatrix.M12) + (localPoint.Y * localToQuadMatrix.M22) + (localPoint.Z * localToQuadMatrix.M32) + localToQuadMatrix.M42;

		//Assert our bounds of the quad (ensure the point is inside)
		if (u is < 0 or > 1 or float.NaN || v is < 0 or > 1 or float.NaN) return null;

		Vector2 uv      = new(u, v);
		bool    outside = normDotDir < 0; //True if hit on the same side as the normal points to

		return new HitRecord(ray, worldPoint, localPoint, Normal, t, outside, uv, this, Material);
	}

	/// <inheritdoc/>
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
		//Inlined code above, since we don't need the Z (normal) coord, and removed the casts to `double`
		float u = (localPoint.X * localToQuadMatrix.M11) + (localPoint.Y * localToQuadMatrix.M21) + (localPoint.Z * localToQuadMatrix.M31) + localToQuadMatrix.M41;
		float v = (localPoint.X * localToQuadMatrix.M12) + (localPoint.Y * localToQuadMatrix.M22) + (localPoint.Z * localToQuadMatrix.M32) + localToQuadMatrix.M42;

		//Assert our bounds of the quad (ensure the point is inside)
		if (u is < 0 or > 1 or float.NaN || v is < 0 or > 1 or float.NaN) return false;

		return true;
	}
}