using RayTracer.Core;
using RayTracer.Core.Acceleration;
using RayTracer.Impl.Materials;
using System.Numerics;

namespace RayTracer.Impl.Hittables;

/// <summary>
///  A hittable that has a constant density (aka a volume like a cloud). Should be used in conjunction with a <see cref="VolumetricMaterial"/>
/// </summary>
/// <remarks>
///  This probably won't work too well with rays inside the medium, or other objects inside it, so beware...
/// </remarks>
public record ConstantDensityMedium(Hittable Boundary, float Density) : Hittable
{
	private readonly float negInvDensity = -1 / Density;

	/// <inheritdoc/>
	public override HitRecord? TryHit(Ray ray, float kMin, float kMax)
	{
		//Where we enter the volume
		if (Boundary.TryHit(ray, kMin, kMax) is not { } hit1) return null;
		//Where we exit the volume
		if (Boundary.TryHit(ray, hit1.K + 0.001f, kMax) is not { } hit2) return null;

		//Find how far we travelled between the inside and outside
		float boundaryDistance = hit2.K - hit1.K;
		//Random distance for how far we're going to travel this time
		float randomHitDistance = negInvDensity * MathF.Log(RandUtils.RandomFloat01());

		if (randomHitDistance > boundaryDistance) return null;

		// ReSharper disable InlineTemporaryVariable
		Vector3    worldPoint = ray.PointAt(randomHitDistance);
		Vector3    localPoint = worldPoint;
		Vector3    normal     = RandUtils.RandomOnUnitSphere(); //Arbitrary
		const bool frontFace  = true;                           //Arbitrary too

		return new HitRecord(ray, worldPoint, localPoint, normal, hit1.K + randomHitDistance, frontFace, Vector2.Zero);
	}

	/// <inheritdoc />
	public override bool FastTryHit(Ray ray, float kMin, float kMax) => Boundary.FastTryHit(ray, kMin, kMax);

	/// <inheritdoc/>
	public override AxisAlignedBoundingBox BoundingVolume => Boundary.BoundingVolume;


}