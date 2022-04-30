using RayTracer.Core.Acceleration;
using System.Numerics;
using static System.Numerics.Vector3;
using static System.MathF;

namespace RayTracer.Core.Hittables;

/// <summary>
///  Bounded version of <see cref="InfinitePlane"/>
/// </summary>
public record Quad(Vector3 A, Vector3 B, Vector3 C, Vector3 D) : Hittable
{
	/// <inheritdoc />
	public override AxisAlignedBoundingBox BoundingVolume { get; } = AxisAlignedBoundingBox.Encompass(A,B,C,D);

	/// <inheritdoc />
	public override HitRecord? TryHit(Ray ray, float kMin, float kMax)
	{
		//Again I'm using IQ's code as reference, since nowhere else seems to have a proper implementation
		//The main problem is how to choose the bounding points and UV coordinates

		Vector3 ba  = B - A; Vector3 pa = p - A;
		Vector3 cb  = C - B; Vector3 pb = p - B;
		Vector3 dc  = D - C; Vector3 pc = p - C;
		Vector3 ad  = A - D; Vector3 pd = p - D;
		Vector3 nor = Cross( ba, ad );

		float distanceToPlane = Sqrt(
				Sign(Dot(Cross(ba, nor), pa)) +
				Sign(Dot(Cross(cb, nor), pb)) +
				Sign(Dot(Cross(dc, nor), pc)) +
				Sign(Dot(Cross(ad, nor), pd)) <3.0
						?
						Min( Min( Min(
												dot2(ba *Clamp(Dot(ba, pa) /dot2(ba), 0.0, 1.0) -pa),
												dot2(cb *Clamp(Dot(cb, pb) /dot2(cb), 0.0, 1.0) -pb) ),
										dot2(dc *Clamp(Dot(dc, pc) /dot2(dc), 0.0, 1.0) -pc) ),
								dot2(ad *Clamp(Dot(ad, pd) /dot2(ad), 0.0, 1.0) -pd) )
						:
						Dot(nor, pa) *Dot(nor, pa) /dot2(nor) );
	}

	private static Vector
}