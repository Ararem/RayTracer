using RayTracer.Core;
using RayTracer.Core.Acceleration;
using System.Numerics;
using static System.Numerics.Vector3;
using static System.MathF;

namespace RayTracer.Impl.Hittables;

/// <summary>A 2D Disk in 3D space. Defined by a point (the centre of the disk) and a normal direction)</summary>
public sealed class Disk : SingleMaterialHittable
{
	/// <summary>-Dot(<see cref="Centre"/>, <see cref="Normal"/>)</summary>
	private readonly float negCentreDotNorm;

	private readonly float radiusSqr;

	private readonly Matrix4x4 localToDiskMatrix;

	/// <summary>A 2D Disk in 3D space. Defined by a point (the centre of the disk) and a normal direction)</summary>
	/// <param name="centre">The centre of the disk in 3D space</param>
	/// <param name="normal">Normal direction of the disk</param>
	/// <param name="radius">How large the radius of the disk is</param>
	public Disk(Vector3 centre, Vector3 normal, float radius)
	{
		normal           = Normalize(normal);
		Centre           = centre;
		Normal           = normal;
		Radius           = radius;
		radiusSqr        = radius * radius;
		BoundingVolume   = new AxisAlignedBoundingBox(centre - new Vector3(radius), centre + new Vector3(radius));
		negCentreDotNorm = -Dot(Centre, Normal);

		//See Quad.cs for a semi explanation of this
		//Since it's essentially the same code, except we find two random UV vectors first
		Vector3 randCrossDir = Abs(Dot(Normal, UnitX)) < 0.01f ? UnitY : UnitX; //Choose the X/Y axis depending on if the X axis is parallel to the normal or not (the Cross fails if we cross two parallel vectors)
		Vector3 u            = Normalize(Cross(Normal, randCrossDir));
		Vector3 v            = Normalize(Cross(Normal, u));
		Matrix4x4 diskToLocalMatrix = new(
				u.X, u.Y, u.Z, 0,
				v.X, v.Y, v.Z, 0,
				Normal.X, Normal.Y, Normal.Z, 0,
				0, 0, 0, 1
		);

		if (!Matrix4x4.Invert(diskToLocalMatrix, out localToDiskMatrix)) throw new ArithmeticException("Could not invert Disk to world transformation matrix (UV coords were probably parallel)");
	}

	//TODO: Very inefficient, need to make AABB smaller and more compact
	/// <inheritdoc/>
	public override AxisAlignedBoundingBox BoundingVolume { get; }

	/// <summary>The centre of the disk in 3D space</summary>
	public Vector3 Centre { get; }

	/// <summary>Normal direction of the disk</summary>
	public Vector3 Normal { get; }

	/// <summary>How large the radius of the disk is</summary>
	public float Radius { get; }

	/// <inheritdoc/>
	//TODO: Disk UV's
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
			t = -(Dot(ray.Origin, Normal) + negCentreDotNorm) / normDotDir;
		}

		//Assert ranges
		if ((t < kMin) || (t > kMax)) return null;

		Vector3 worldPoint = ray.PointAt(t);

		//Now assert radius
		if (DistanceSquared(Centre, worldPoint) > radiusSqr) return null;

		Vector3 localPoint = worldPoint - Centre;
		bool    outside    = normDotDir < 0; //True if hit on the same side as the normal points to
		// Vector3 uvn = Transform(localPoint, LocalToQuadMatrix);
		// float   u   = uvn.X, v = uvn.Y;
		//Inlined code above, since we don't need the Z coord, and removed the casts to `double`
		float u = (localPoint.X * localToDiskMatrix.M11) + (localPoint.Y * localToDiskMatrix.M21) + (localPoint.Z * localToDiskMatrix.M31) + localToDiskMatrix.M41;
		float v = (localPoint.X * localToDiskMatrix.M12) + (localPoint.Y * localToDiskMatrix.M22) + (localPoint.Z * localToDiskMatrix.M32) + localToDiskMatrix.M42;

		Vector2 uv = new(u, v);

		//Assert our bounds of the quad (ensure the point is inside)
		if (u is < 0 or > 1 or float.NaN || v is < 0 or > 1 or float.NaN) uv = Vector2.Clamp(uv, Vector2.Zero, Vector2.One);


		return new HitRecord(ray, worldPoint, localPoint, Normal, t, outside, uv, this, Material);
	}

	/// <inheritdoc/>
	public override bool FastTryHit(Ray ray, float kMin, float kMax)
	{
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
			t = -(Dot(ray.Origin, Normal) + negCentreDotNorm) / normDotDir;
		}

		//Assert ranges
		if ((t < kMin) || (t > kMax)) return false;

		Vector3 worldPoint = ray.PointAt(t);

		//Now assert radius
		return DistanceSquared(Centre, worldPoint) <= radiusSqr;
	}
}