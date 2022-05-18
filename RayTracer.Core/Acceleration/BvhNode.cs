namespace RayTracer.Core.Acceleration;

/// <summary>Interface defining a node on a <see cref="BvhTree"/></summary>
/// <param name="RenderStats">Render statistics used to track things...</param>
public abstract record BvhNode(RenderStats RenderStats)
{
	/// <summary>Bounding box that encompasses this <see cref="BvhNode"/> and all it's children nodes (if any)</summary>
	/// <remarks>Used to quickly discard nodes where a given <see cref="Ray"/> definitely won't intersect</remarks>
	public abstract AxisAlignedBoundingBox BoundingBox { get; }

	/// <inheritdoc cref="Hittable.TryHit"/>
	public abstract (SceneObject Object, HitRecord Hit)? TryHit(Ray ray, float kMin, float kMax);

	/// <inheritdoc cref="AsyncRenderJob.AnyIntersectionFast"/>
	public abstract bool FastTryHit(Ray ray, float kMin, float kMax);
}