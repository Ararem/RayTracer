using RayTracer.Core.Graphics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using static System.Numerics.Vector3;

namespace RayTracer.Core.Hittables;

/// <summary>
/// A simple 3D box
/// </summary>
/// <remarks>Made up of 6 planes, hence the name</remarks>
public record HexPlaneBox : Hittable
{
	/// <summary>
	/// Creates a new box from two bounding points
	/// </summary>
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

				new YZPlane(Min.Y, Max.Y, Min.Z, Max.Z, Min.Y),
				new YZPlane(Min.Y, Max.Y, Min.Z, Max.Z, Max.Y)
		};
	}

	/// <summary>
	/// Min XYZ values for the box
	/// </summary>
	public Vector3 Min { get;}

	/// <summary>
	/// Max XYZ values for the box
	/// </summary>
	public Vector3 Max { get;}

	private readonly Hittable[] sides;


	/// <summary>
	/// Record method implementation
	/// </summary>
	/// <param name="Min"></param>
	/// <param name="Max"></param>
	[SuppressMessage("ReSharper", "ParameterHidesMember")]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public void Deconstruct(out Vector3 Min, out Vector3 Max)
	{
		Min = this.Min;
		Max = this.Max;
	}

	/// <inheritdoc />
	public override HitRecord? TryHit(Ray ray, float kMin, float kMax)
	{
		HitRecord? closest = null;
		for (int i = 0; i < sides.Length; i++)
		{
			HitRecord? hit =sides[i].TryHit(ray, kMin, kMax);
			if(hit is null) continue;
			//First hit is always the closest
			closest ??= hit;
			//Assign if closer
			if (hit.Value.K < closest.Value.K) closest = hit;
		}

		//TODO: Uv coords are local to the face they hit
		return closest;
	}
}