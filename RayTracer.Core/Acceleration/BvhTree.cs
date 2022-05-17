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
	///  Root <see cref="BvhNode"/> for this BVH Tree
	/// </summary>
	public readonly BvhNode RootNode;

	private readonly RenderStats renderStats;

	/// <summary>
	///  Creates a new BVH tree for the specified scene
	/// </summary>
	/// <param name="scene">Scene to create the BVH tree for</param>
	/// <param name="renderStats">Object used for tracking render statistics</param>
	public BvhTree(Scene scene, RenderStats renderStats)
	{
		Log.Debug("Creating new Bvh Tree for scene {Scene}", scene);
		// RootNode = FromArraySegment(scene.SceneObjects);
		/*
		 * Interesting side-note, using SAH as opposed to the plain "split in the middle" approach is really effective
		 * In the RayTracing in a Weekend Book 1 demo scene, it cuts down the render times from 2:00 hours to ~1:25, which is a really good 25% speedup
		 */
		this.renderStats = renderStats;
		RootNode         = FromSegment_SAH(scene.SceneObjects, 0);
	}

	/// <inheritdoc cref="IHittable.TryHit"/>
	public (SceneObject Object, HitRecord Hit)? TryHit(Ray ray, float kMin, float kMax) => RootNode.TryHit(ray, kMin, kMax);

	private BvhNode FromSegment_SAH(ArraySegment<SceneObject> segment, int depth)
	{
		//Simple check if 1 element so we can assume more than 1 later on
		if (segment.Count == 1) return new SingleObjectBvhNode(segment[0], renderStats);

		//Port of Pete Shirley's code
		// https://psgraphics.blogspot.com/2016/03/a-simple-sah-bvh-build.html
		// https://3.bp.blogspot.com/-PMG6dWk1i60/VuG9UHjsdlI/AAAAAAAACEo/BS1qJyut7LE/s1600/Screen%2BShot%2B2016-03-10%2Bat%2B11.25.08%2BAM.png

		int                      n         = segment.Count;
		AxisAlignedBoundingBox[] boxes     = new AxisAlignedBoundingBox[n];
		float[]                  leftArea  = new float[n];
		float[]                  rightArea = new float[n];
		SceneObject[]            objects   = segment.ToArray();

		AxisAlignedBoundingBox mainBox = segment[0].Hittable.BoundingVolume;
		for (int i = 1; i < n; i++)
		{
			mainBox = AxisAlignedBoundingBox.Encompass(segment[i].Hittable.BoundingVolume, mainBox);
		}

		//Find longest axis to split along, then sort
		Vector3 size = mainBox.Max - mainBox.Min;
		float   max  = MathF.Max(MathF.Max(size.X, size.Y), size.Z);
		int     axis = Math.Abs(max - size.X) < 0.001f ? 0 : Math.Abs(max - size.Y) < 0.001f ? 1 : 2; //Choose longest axis

		Comparison<SceneObject> compareFunc = axis switch
		{
				0 => static (a, b) => CompareHittables(a.Hittable.BoundingVolume, b.Hittable.BoundingVolume, static v => v.X),
				1 => static (a, b) => CompareHittables(a.Hittable.BoundingVolume, b.Hittable.BoundingVolume, static v => v.Y),
				_ => static (a, b) => CompareHittables(a.Hittable.BoundingVolume, b.Hittable.BoundingVolume, static v => v.Z)
		};
		Array.Sort(objects, compareFunc);

		//Copy AABB's to an array for easier access
		for (int i = 0; i < n; i++) boxes[i] = segment[i].Hittable.BoundingVolume;

		//Calculate the area from the left towards right
		leftArea[0] = GetAABBArea(boxes[0]);
		AxisAlignedBoundingBox leftBox = boxes[0];
		for (int i = 1; i < n - 1; i++)
		{
			leftBox     = AxisAlignedBoundingBox.Encompass(leftBox, boxes[i]);
			leftArea[i] = GetAABBArea(leftBox);
		}

		//Calculate the area from right towards left
		rightArea[n - 1] = GetAABBArea(boxes[n - 1]);
		AxisAlignedBoundingBox rightBox = boxes[n - 1];
		for (int i = n - 2; i > 0; i--)
		{
			rightBox     = AxisAlignedBoundingBox.Encompass(rightBox, boxes[i]);
			rightArea[i] = GetAABBArea(rightBox);
		}

		//Find the index at which we get the smallest surface area, in order to find the most optimal split
		float minSA      = float.MaxValue;
		int   minSAIndex = 0;
		for (int i = 0; i < n - 1; i++)
		{
			float sah = (i * leftArea[i]) + ((n - i - 1) * rightArea[i + 1]);
			if (sah < minSA)
			{
				minSAIndex = i;
				minSA      = sah;
			}
		}

		//We know we'll be using binary nodes because we already checked for a single object earlier
		string indent = new(' ', depth);
		Log.Verbose("{Indent}Split at {SplitPosition}/{Count} along {Axis} axis", indent, minSAIndex, objects.Length, axis switch { 0 => 'X', 1 => 'Y', _ => 'Z' });

		BvhNode leftNode  = minSAIndex == 0 ? new SingleObjectBvhNode(objects[0],                  renderStats) : FromSegment_SAH(objects[..(minSAIndex + 1)], depth + 1);
		BvhNode rightNode = minSAIndex == n - 2 ? new SingleObjectBvhNode(objects[minSAIndex + 1], renderStats) : FromSegment_SAH(objects[(minSAIndex   + 1)..], depth + 1);

		return new BinaryBvhNode(leftNode, rightNode, renderStats);

		static float GetAABBArea(AxisAlignedBoundingBox aabb)
		{
			Vector3 size = aabb.Max - aabb.Min;
			return 2 * ((size.X * size.Y) + (size.Y * size.Z) + (size.Z * size.X));
		}
	}


	//Old version of splitting code
	// private static IBvhNode FromArraySegment(ArraySegment<SceneObject> segment)
	// {
	// 	//Have to copy the list since we'll be modifying it
	// 	SceneObject[] objects = segment.ToArray();
	//
	// 	//Choose a random axis to sort and split along
	// 	int axis = RandUtils.RandomInt(0, 3);
	// 	Log.Verbose("Split Axis is {Axis}", axis switch { 0 => 'X', 1 => 'Y', _ => 'Z' });
	// 	Comparison<SceneObject> compareFunc = axis switch
	// 	{
	// 			0 => static (a, b) => CompareHittables(a.Hittable.BoundingVolume, b.Hittable.BoundingVolume, static v => v.X),
	// 			1 => static (a, b) => CompareHittables(a.Hittable.BoundingVolume, b.Hittable.BoundingVolume, static v => v.Y),
	// 			_ => static (a, b) => CompareHittables(a.Hittable.BoundingVolume, b.Hittable.BoundingVolume, static v => v.Z)
	// 	};
	//
	// 	IBvhNode node;
	// 	switch (segment.Count)
	// 	{
	// 		case 1:
	// 			node = new SingleObjectBvhNode(segment[0]);
	// 			break;
	// 		case 2:
	// 			node = new BinaryBvhNode(new SingleObjectBvhNode(segment[0]), new SingleObjectBvhNode(segment[1]));
	// 			break;
	// 		//Recursively split the objects again
	// 		default:
	// 			Array.Sort(objects, compareFunc);
	// 			int      mid = objects.Length / 2;
	// 			IBvhNode a   = FromArraySegment(objects[..mid]);
	// 			IBvhNode b   = FromArraySegment(objects[mid..]);
	// 			node = new BinaryBvhNode(a, b);
	// 			break;
	// 	}
	//
	// 	return node;
	// }

	/// <summary>
	///  Compares two <see cref="IHittable">Hittables</see> (by comparing their extremes). Used for splitting the scene along an axis
	/// </summary>
	/// <param name="a">First hittable to compare</param>
	/// <param name="b">Second hittable to compare</param>
	/// <param name="getAxis">Function to get the given axis value to compare along (e.g. <c>v => v.X</c>)</param>
	[SuppressMessage("ReSharper", "UseDeconstructionOnParameter")]
	private static int CompareHittables(AxisAlignedBoundingBox a, AxisAlignedBoundingBox b, [RequireStaticDelegate] Func<Vector3, float> getAxis)
	{
		//So it would be better to sort by the min/max bounds like below, but that completely fails with infinities
		//So i have to sort using the centre instead :(
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