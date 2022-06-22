using RayTracer.Core;

namespace RayTracer.Impl.Lights;

/// <inheritdoc />
public sealed class PointLight : SimpleLightBase
{
	/// <inheritdoc />
	protected override (Ray ray, float kMin, float kMax) GetShadowRayForHit(HitRecord hit) => DefaultGetShadowRayForHit(hit, Position);
}