using RayTracer.Core.Hittables;

namespace RayTracer.Core.Acceleration;

/// <summary>
///  Implementation of <see cref="IBvhNode"/> for two sub-nodes
/// </summary>
/// <param name="NodeA">First sub-node</param>
/// <param name="NodeB">Second sub-node</param>
public sealed record BinaryBvhNode(IBvhNode NodeA, IBvhNode NodeB) : IBvhNode
{
	/// <inheritdoc/>
	public AxisAlignedBoundingBox BoundingBox { get; } = AxisAlignedBoundingBox.Encompass(NodeA.BoundingBox, NodeB.BoundingBox);

	/// <inheritdoc/>
	public (SceneObject Object, HitRecord Hit)? TryHit(Ray ray, float kMin, float kMax)
	{
		//Quit early if we miss the bounding volume
		if (BoundingBox.Hit(ray, kMin, kMax) == false) return null;

		(SceneObject Object, HitRecord Hit)? maybeHitA = NodeA.TryHit(ray, kMin, kMax);
		//If we hit node A, we still need to check if node B is closer
		if (maybeHitA is { } hitA)
		{
			//Check node B up to where node A intersected
			//This way if node B has an intersection, we know that it has to be closer
			(SceneObject Object, HitRecord Hit)? maybeHitB = NodeB.TryHit(ray, kMin, hitA.Hit.K);
			if (maybeHitB is { } hitB) return hitB;
			else return hitA;
		}
		else
		{
			//Node A wasn't hit so just check node B
			return NodeB.TryHit(ray, kMin, kMax);
		}
	}
}