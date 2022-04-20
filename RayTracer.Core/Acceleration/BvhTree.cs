using JetBrains.Annotations;
using RayTracer.Core.Hittables;
using Serilog;
using System.Data;
using System.Numerics;

namespace RayTracer.Core.Acceleration;

public sealed class BvhTree
{
	/// <inheritdoc cref="Hittable.TryHit"/>
	public (SceneObject Object, HitRecord Hit)? TryHit(Ray ray, float kMin, float kMax) => RootNode.TryHit(ray, kMin, kMax);

	/// <summary>
	/// Root <see cref="IBvhNode"/> for this BVH Tree
	/// </summary>
	public readonly IBvhNode RootNode;

	/// <summary>
	/// Creates a new BVH tree for the specified scene
	/// </summary>
	/// <param name="scene"></param>
	public BvhTree(Scene scene)
	{
		Log.Debug("Creating new Bvh Tree for scene {Scene}", scene);
		RootNode = FromArraySegment(scene.SceneObjects);
	}

	private static IBvhNode FromArraySegment(ArraySegment<SceneObject> segment)
	{
		//Have to copy the list since we'll be modifying it
		SceneObject[] objects = segment.ToArray();
		Log.Verbose("Segment is {Segment}", segment);

		//Choose a random axis to sort and split along
		int axis = RandUtils.RandomInt(0, 3);
		Log.Verbose("Split Axis is {Axis}", axis switch { 0 => 'X', 1 => 'Y', _ => 'Z' });
		Comparison<SceneObject> compareFunc = axis switch
		{
				0 => static (a,b) => CompareHittables(a.Hittable,b.Hittable, static v => v.X),
				1 => static (a,b) => CompareHittables(a.Hittable,b.Hittable, static v => v.Y),
				_ => static (a,b) => CompareHittables(a.Hittable,b.Hittable, static v => v.Z),
		};

		IBvhNode node;
		switch (segment.Count)
		{
			case 1:
				node= new SingleObjectBvhNode(segment[0]);
				break;
			case 2:
				node = new BinaryBvhNode(new SingleObjectBvhNode(segment[0]), new SingleObjectBvhNode(segment[1]));
				break;
			//Recursively split the objects again
			default:
				Array.Sort(objects, compareFunc);
				int      mid = objects.Length / 2;
				IBvhNode a   = FromArraySegment(objects[..mid]);
				IBvhNode b   = FromArraySegment(objects[mid..]);
				node= new BinaryBvhNode(a, b);
				break;
		}

		Log.Verbose("Node is {Node}", node);
		return node;
	}

	/// <summary>
	/// Compares two <see cref="Hittable">Hittables</see> (by comparing their extremes). Used for splitting the scene along an axis
	/// </summary>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <param name="getAxis">Function to get the </param>
	/// <returns></returns>
	/// <exception cref="NoNullAllowedException"></exception>
	private static int CompareHittables(Hittable a, Hittable b, [RequireStaticDelegate] Func<Vector3, float> getAxis)
	{
		//What I'm trying to do here is to make the comparison work for hittables that don't really have bounds (think ∞∞∞∞∞∞∞∞∞∞∞∞)
		Vector3 aMin = a.BoundingVolume?.Min ?? new Vector3(float.NegativeInfinity);
		Vector3 bMax = b.BoundingVolume?.Max ?? new Vector3(float.PositiveInfinity);

		return getAxis(aMin).CompareTo(getAxis(bMax));
	}
}