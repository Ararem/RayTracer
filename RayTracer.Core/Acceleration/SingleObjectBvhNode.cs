using RayTracer.Core.Hittables;

namespace RayTracer.Core.Acceleration;

/// <summary>
///  Bvh node for a singular object
/// </summary>
public sealed record SingleObjectBvhNode(SceneObject SceneObject, AsyncRenderJob ParentJob) : BvhNode(ParentJob)
{
	/// <inheritdoc/>
	public override (SceneObject Object, HitRecord Hit)? TryHit(Ray ray, float kMin, float kMax)
	{
		//Skip early if AABB miss
		if (!BoundingBox.Hit(ray, kMin, kMax))
		{
			Interlocked.Increment(ref ParentJob.RenderStats.AabbMisses);
			return null;
		}
		else
		{
			HitRecord? maybeHit = SceneObject.Hittable.TryHit(ray, kMin, kMax);
			if (maybeHit is { } hit)
			{
				Interlocked.Increment(ref ParentJob.RenderStats.HittableIntersections);
				return (SceneObject, hit);
			}
			else
			{
				Interlocked.Increment(ref ParentJob.RenderStats.HittableMisses);
				return null;
			}
		}
	}

	/// <inheritdoc />
	public override bool AnyIntersection(Ray ray, float kMin, float kMax) => TryHit(ray, kMin, kMax) != null;

	/// <inheritdoc/>
	public override AxisAlignedBoundingBox BoundingBox { get; } = SceneObject.Hittable.BoundingVolume;

	/// <summary>
	///  Implicit operator to create a new <see cref="SingleObjectBvhNode"/> from a <see cref="SceneObject"/>. Simply calls the default constructor (
	///  <see cref="SingleObjectBvhNode(RayTracer.Core.SceneObject)"/>)
	/// </summary>
	/// <param name="obj">Scene object to create a node for</param>
	public static implicit operator SingleObjectBvhNode(SceneObject obj) => new(obj);
}