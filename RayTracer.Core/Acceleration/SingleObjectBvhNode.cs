using RayTracer.Core.Hittables;

namespace RayTracer.Core.Acceleration;

/// <summary>
///  Bvh node for a singular object
/// </summary>
public sealed record SingleObjectBvhNode(SceneObject SceneObject, RenderStats RenderStats) : BvhNode(RenderStats)
{
	/// <inheritdoc/>
	public override (SceneObject Object, HitRecord Hit)? TryHit(Ray ray, float kMin, float kMax)
	{
		//Skip early if AABB miss
		if (!BoundingBox.Hit(ray, kMin, kMax))
		{
			Interlocked.Increment(ref RenderStats.AabbMisses);
			return null;
		}
		else
		{
			HitRecord? maybeHit = SceneObject.Hittable.TryHit(ray, kMin, kMax);
			if (maybeHit is { } hit)
			{
				Interlocked.Increment(ref RenderStats.HittableIntersections);
				return (SceneObject, hit);
			}
			else
			{
				Interlocked.Increment(ref RenderStats.HittableMisses);
				return null;
			}
		}
	}

	/// <inheritdoc />
	public override bool FastTryHit(Ray ray, float kMin, float kMax) => SceneObject.Hittable.FastTryHit(ray, kMin, kMax);

	/// <inheritdoc/>
	public override AxisAlignedBoundingBox BoundingBox { get; } = SceneObject.Hittable.BoundingVolume;
}