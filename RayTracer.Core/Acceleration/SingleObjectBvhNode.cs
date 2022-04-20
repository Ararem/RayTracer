using RayTracer.Core.Hittables;

namespace RayTracer.Core.Acceleration;

/// <summary>
///  Bvh node for a singular object
/// </summary>
public sealed record SingleObjectBvhNode(SceneObject SceneObject) : IBvhNode
{
	/// <inheritdoc/>
	public (SceneObject Object, HitRecord Hit)? TryHit(Ray ray, float kMin, float kMax)
	{
		if (!BoundingBox.Hit(ray, kMin, kMax))
		{
			return null;
		}
		else
		{
			HitRecord? maybeHit = SceneObject.Hittable.TryHit(ray, kMin, kMax);
			return maybeHit is { } hit ? (SceneObject, hit) : null;
		}
	}

	/// <inheritdoc/>
	public AxisAlignedBoundingBox BoundingBox { get; } = SceneObject.Hittable.BoundingVolume;

	/// <summary>
	///  Implicit operator to create a new <see cref="SingleObjectBvhNode"/> from a <see cref="SceneObject"/>. Simply calls the default constructor (
	///  <see cref="SingleObjectBvhNode(RayTracer.Core.SceneObject)"/>)
	/// </summary>
	/// <param name="obj">Scene object to create a node for</param>
	public static implicit operator SingleObjectBvhNode(SceneObject obj) => new(obj);
}