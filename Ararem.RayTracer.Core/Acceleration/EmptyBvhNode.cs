using System.Numerics;

namespace Ararem.RayTracer.Core.Acceleration;

/// <summary>Implementation of <see cref="BvhNode"/> that doesn't do anything</summary>
/// <param name="RenderStats"></param>
public record EmptyBvhNode(RenderStats RenderStats) : BvhNode(RenderStats)
{
	/// <inheritdoc/>
	public override AxisAlignedBoundingBox BoundingBox { get; } = new(Vector3.Zero, Vector3.Zero);

	/// <inheritdoc/>
	public override (SceneObject Object, HitRecord Hit)? TryHit(Ray ray, float kMin, float kMax) => null;

	/// <inheritdoc/>
	public override bool FastTryHit(Ray ray, float kMin, float kMax) => false;
}