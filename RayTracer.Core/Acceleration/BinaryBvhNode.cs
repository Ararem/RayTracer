using RayTracer.Core.Hittables;

namespace RayTracer.Core.Acceleration;

public record BinaryBvhNode(IBvhNode NodeA, IBvhNode NodeB) : IBvhNode
{
	/// <inheritdoc />
	public AxisAlignedBoundingBox BoundingBox { get; } = AxisAlignedBoundingBox.Encompass(NodeA.BoundingBox, NodeB.BoundingBox);

	/// <inheritdoc />
	public HitRecord? TryHit(Ray ray, float kMin, float kMax)
	{
		//Quit early if we miss the bounding volume
		if (BoundingBox.Hit(ray, kMin, kMax) == false) return null;

		HitRecord? maybeHitA = NodeA.TryHit(ray, kMin, kMax);
		//If we hit node A, we still need to check if node B is closer
		if (maybeHitA is { } hitA)
		{
			//Check node B up to where node A intersected
			//This way if node B has an intersection, we know that it has to be closer
			HitRecord? maybeHitB = NodeB.TryHit(ray, kMin, hitA.K);
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