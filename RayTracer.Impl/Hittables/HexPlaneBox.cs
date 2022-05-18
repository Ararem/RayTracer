using RayTracer.Core;
using RayTracer.Core.Acceleration;
using System.Numerics;
using static System.Numerics.Vector3;

namespace RayTracer.Impl.Hittables;

/// <summary>A simple 3D box</summary>
/// <remarks>Made up of 6 planes, hence the name</remarks>
public class HexPlaneBox : Hittable
{
	private readonly Hittable[] sides;

	/// <summary>Creates a new box from two bounding points</summary>
	public HexPlaneBox(Vector3 min, Vector3 max)
	{
		Min = Min(min, max);
		Max = Max(min, max);

		sides = new Hittable[6]
		{
				new XYPlane(Min.X, Max.X, Min.Y, Max.Y, Min.Z),
				new XYPlane(Min.X, Max.X, Min.Y, Max.Y, Max.Z),

				new XZPlane(Min.X, Max.X, Min.Z, Max.Z, Min.Y),
				new XZPlane(Min.X, Max.X, Min.Z, Max.Z, Max.Y),

				new YZPlane(Min.Y, Max.Y, Min.Z, Max.Z, Min.X),
				new YZPlane(Min.Y, Max.Y, Min.Z, Max.Z, Max.X)
		};
		BoundingVolume = new AxisAlignedBoundingBox(Min, Max);
	}

	/// <summary>Min XYZ values for the box</summary>
	public Vector3 Min { get; }

	/// <summary>Max XYZ values for the box</summary>
	public Vector3 Max { get; }

	/// <inheritdoc/>
	public override AxisAlignedBoundingBox BoundingVolume { get; }

	/// <inheritdoc/>
	public override HitRecord? TryHit(Ray ray, float kMin, float kMax)
	{
		HitRecord? closest = null;
		for (int i = 0; i < sides.Length; i++)
		{
			HitRecord? hit = sides[i].TryHit(ray, kMin, kMax);
			if (hit is null) continue;
			//First hit is always the closest
			closest ??= hit;
			//Assign if closer
			if (hit.Value.K < closest.Value.K) closest = hit;
		}

		//TODO: Uv coords are local to the face they hit
		return closest;
	}

	/// <inheritdoc/>
	public override bool FastTryHit(Ray ray, float kMin, float kMax)
	{
		for (int i = 0; i < sides.Length; i++)
			if (sides[i].FastTryHit(ray, kMin, kMax))
				return true;

		return false;
	}
}