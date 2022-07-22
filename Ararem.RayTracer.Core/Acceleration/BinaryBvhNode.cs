namespace Ararem.RayTracer.Core.Acceleration;

/// <summary>Implementation of <see cref="BvhNode"/> for two sub-nodes</summary>
/// <param name="NodeA">First sub-node</param>
/// <param name="NodeB">Second sub-node</param>
/// <param name="RenderStats"><see cref="RenderStats"/> object used to track statistics for this node</param>
public sealed record BinaryBvhNode(BvhNode NodeA, BvhNode NodeB, RenderStats RenderStats) : BvhNode(RenderStats)
{
	/// <inheritdoc/>
	public override AxisAlignedBoundingBox BoundingBox { get; } = AxisAlignedBoundingBox.Encompass(NodeA.BoundingBox, NodeB.BoundingBox);

	/// <inheritdoc/>
	public override (SceneObject Object, HitRecord Hit)? TryHit(Ray ray, float kMin, float kMax)
	{
		//Quit early if we miss the bounding volume
		if (BoundingBox.Hit(ray, kMin, kMax) == false)
		{
			Interlocked.Increment(ref RenderStats.AabbMisses);
			return null;
		}

		(SceneObject Object, HitRecord Hit)? maybeHitA = NodeA.TryHit(ray, kMin, kMax);
		//If we hit node A, we still need to check if node B is closer
		if (maybeHitA is {} hitA)
		{
			//Check node B up to where node A intersected
			//This way if node B has an intersection, we know that it has to be closer
			(SceneObject Object, HitRecord Hit)? maybeHitB = NodeB.TryHit(ray, kMin, hitA.Hit.K);
			if (maybeHitB is {} hitB) return hitB;
			else return hitA;
		}
		else
		{
			//Node A wasn't hit so just check node B
			return NodeB.TryHit(ray, kMin, kMax);
		}
	}

	/// <inheritdoc/>
	//Cool that the short-circuiting operator means we can skip half the tree if we get a positive
	public override bool FastTryHit(Ray ray, float kMin, float kMax) => NodeA.FastTryHit(ray, kMin, kMax) || NodeB.FastTryHit(ray, kMin, kMax);
}