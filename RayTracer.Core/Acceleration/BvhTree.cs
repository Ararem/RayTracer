using JetBrains.Annotations;
using RayTracer.Core.Hittables;
using Serilog;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace RayTracer.Core.Acceleration;

/// <summary>
///  Class that handles creating a Bounding Volume Hierarchy (BVH) tree for a given <see cref="Scene"/>. This can be used to accelerate ray-object
///  intersections by quickly discarding a <see cref="Ray"/> that will not intersect a given objects (since it doesn't intersect it's bounds)
/// </summary>
public sealed class BvhTree
{
	/// <summary>
	///  Root <see cref="IBvhNode"/> for this BVH Tree
	/// </summary>
	public readonly IBvhNode RootNode;

	/// <summary>
	///  Creates a new BVH tree for the specified scene
	/// </summary>
	/// <param name="scene"></param>
	public BvhTree(Scene scene)
	{
		Log.Debug("Creating new Bvh Tree for scene {Scene}", scene);
		RootNode = FromArraySegment(scene.SceneObjects);
	}

	/// <inheritdoc cref="Hittable.TryHit"/>
	public (SceneObject Object, HitRecord Hit)? TryHit(Ray ray, float kMin, float kMax) => RootNode.TryHit(ray, kMin, kMax);

	private static IBvhNode FromArraySegment(ArraySegment<SceneObject> segment)
	{
		//Have to copy the list since we'll be modifying it
		SceneObject[] objects = segment.ToArray();

		//Choose a random axis to sort and split along
		int axis = RandUtils.RandomInt(0, 3);
		Log.Verbose("Split Axis is {Axis}", axis switch { 0 => 'X', 1 => 'Y', _ => 'Z' });
		Comparison<SceneObject> compareFunc = axis switch
		{
				0 => static (a, b) => CompareHittables(a.Hittable.BoundingVolume, b.Hittable.BoundingVolume, static v => v.X),
				1 => static (a, b) => CompareHittables(a.Hittable.BoundingVolume, b.Hittable.BoundingVolume, static v => v.Y),
				_ => static (a, b) => CompareHittables(a.Hittable.BoundingVolume, b.Hittable.BoundingVolume, static v => v.Z)
		};

		IBvhNode node;
		switch (segment.Count)
		{
			case 1:
				node = new SingleObjectBvhNode(segment[0]);
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
				node = new BinaryBvhNode(a, b);
				break;
		}
		return node;
	}

	/// <summary>
	///  Compares two <see cref="Hittable">Hittables</see> (by comparing their extremes). Used for splitting the scene along an axis
	/// </summary>
	/// <param name="a">First hittable to compare</param>
	/// <param name="b">Second hittable to compare</param>
	/// <param name="getAxis">Function to get the given axis value to compare along (e.g. <c>v => v.X</c>)</param>
	[SuppressMessage("ReSharper", "UseDeconstructionOnParameter")]
	private static int CompareHittables(AxisAlignedBoundingBox a, AxisAlignedBoundingBox b, [RequireStaticDelegate] Func<Vector3, float> getAxis)
	{
		//So it would be better to sort by the min/max bounds like below, but that completely fails with infinities
		//So i have to sort using the centre instead :(
		//TODO: Implement the SAH method outlined here
		//https://3.bp.blogspot.com/-PMG6dWk1i60/VuG9UHjsdlI/AAAAAAAACEo/BS1qJyut7LE/s1600/Screen%2BShot%2B2016-03-10%2Bat%2B11.25.08%2BAM.png
		//
		// ReSharper disable once ArrangeMethodOrOperatorBody
		return getAxis((a.Min + a.Max) / 2f).CompareTo(getAxis((b.Min + b.Min) / 2f));

		// if (a.Equals(b)) return 0;
		// if (ReferenceEquals(a, b) && ReferenceEquals(a, AxisAlignedBoundingBox.Infinite)) return 0;
		//
		// Vector3 aMin = a.Min;
		// Vector3 bMax = b.Max;
		//
		// return getAxis(aMin).CompareTo(getAxis(bMax));
	}
}