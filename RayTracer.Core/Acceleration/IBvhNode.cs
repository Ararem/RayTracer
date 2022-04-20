using RayTracer.Core.Hittables;

namespace RayTracer.Core.Acceleration;

/// <summary>
/// Interface defining a node on a <see cref="BvhTree"/>
/// </summary>
public interface IBvhNode
{
	/// <inheritdoc cref="Hittable.TryHit"/>
	public (SceneObject Object, HitRecord Hit)? TryHit(Ray ray, float kMin, float kMax);

	/// <summary>
	/// Bounding box that encompasses this <see cref="IBvhNode"/> and all it's children nodes (if any)
	/// </summary>
	/// <remarks>Used to quickly discard nodes where a given <see cref="Ray"/> definitely won't intersect</remarks>
	public AxisAlignedBoundingBox BoundingBox { get; }
}