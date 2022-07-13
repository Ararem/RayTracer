using LibArarem.Core.Exceptions;
using RayTracer.Core.Debugging;
using System.ComponentModel;

namespace RayTracer.Core;

/// <summary>Class that contains properties that are used to control how a <see cref="Scene"/> is rendered</summary>
//TODO: Should we add property checks to these?
public sealed class RenderOptions
{
	private readonly int renderHeight = 1080;
	private readonly int renderWidth  = 1920;

	private int concurrencyLevel = Environment.ProcessorCount;

	private float kMax = float.PositiveInfinity;

	private float kMin = 0.001f;

	/// <summary>How many pixels wide the render will be</summary>
	public int RenderWidth
	{
		get => renderWidth;
		init
		{
			if (value < 1) throw new ArgumentOutOfRangeException<int>(value, "Image cannot have a width of less than 1 pixel", nameof(RenderWidth), (1, int.MaxValue));
			renderWidth = value;
		}
	}

	/// <summary>How many pixels high the render will be</summary>
	public int RenderHeight
	{
		get => renderHeight;
		init
		{
			if (value < 1) throw new ArgumentOutOfRangeException<int>(value, "Image cannot have a height of less than 1 pixel", nameof(RenderHeight), (1, int.MaxValue));
			renderHeight = value;
		}
	}

	/// <summary>Minimum K value for intersections to be considered valid (Near plane)</summary>
	/// <remarks>
	///  <c>K</c> values mean the distance along a given ray at which the intersection occured. This essentially means what is the closest hit that we
	///  consider valid. Set this to slightly above 0 to avoid self-intersecting problems, such as shadow acne. Note that this distance is distance along the
	///  <see cref="Ray"/>, not distance from the <see cref="Camera"/>
	/// </remarks>
	public float KMin
	{
		get => kMin;
		set
		{
			if (value < 1) throw new ArgumentOutOfRangeException<float>(value, "KMin cannot have a value of less than 0", nameof(KMin), (0f, float.PositiveInfinity));
			kMin = value;
		}
	}

	/// <summary>Maximum K values for intersections to be considered valid (Far plane)</summary>
	/// <remarks>
	///  <c>K</c> values mean the distance along a given ray at which the intersection occured. This essentially means what is the furthest hit that we
	///  consider valid. Note that this distance is distance along the <see cref="Ray"/>, not distance from the <see cref="Camera"/>. This should also be
	///  &gt; <see cref="KMin"/>, preferably a very large number such as <see cref="float.PositiveInfinity"/>
	/// </remarks>
	public float KMax
	{
		get => kMax;
		set
		{
			if ((value < KMin) || (value < 0)) throw new ArgumentOutOfRangeException<float>(value, "KMax cannot have a value of less than 0 or less than KMin", nameof(KMax), (0f, float.PositiveInfinity), (KMin, float.PositiveInfinity));
			kMax = value;
		}
	}

	/// <summary>Maximum number of threads that can render concurrently</summary>
	/// <remarks>
	///  Defaults to the number of (hyper-threaded) cores present (see <see cref="Environment.ProcessorCount"/>). Set this to -1 for an unlimited number of
	///  threads, or any other positive integer to limit to that many threads (0 is invalid)
	/// </remarks>
	public int ConcurrencyLevel
	{
		get => concurrencyLevel;
		set
		{
			if (value is < -1 or 0) throw new ArgumentOutOfRangeException<int>(value, $"{nameof(ConcurrencyLevel)} was negative (should be -1 or >0).", nameof(ConcurrencyLevel), (-1, -1), (1, int.MaxValue));
			concurrencyLevel = value;
		}
	}

	private int passes = 100;

	/// <summary>Number of times the image will be rendered. These individual renders ('passes') will be combined to create the final image</summary>
	/// <remarks>If equal to <c>-1</c>, the renderer will render infinitely until manually stopped. No other negative values are acceptable (and neither is zero)</remarks>
	public int Passes
	{
		get => passes;
		set {
			if (value is < -1 or 0) throw new ArgumentOutOfRangeException<int>(value, $"{nameof(Passes)} was negative (should be -1 or >0).", nameof(Passes), (-1, -1), (1, int.MaxValue));
			passes = value;
		}
	}

	private int maxBounceDepth = 100;

	/// <summary>
	///  Maximum number of bounces allowed before a given ray path is discarded. Essentially puts a limit on how many times a ray can bounce, to avoid
	///  infinite bouncing
	/// </summary>
	/// <remarks>Must be &gt;=0</remarks>
	public int MaxBounceDepth
	{
		get => maxBounceDepth;
		set
		{
			if (maxBounceDepth < 0) throw new ArgumentOutOfRangeException<int>(maxBounceDepth, $"{nameof(MaxBounceDepth)} must be >=0", nameof(MaxBounceDepth), (0, int.MaxValue));
			maxBounceDepth = value;
		}
	}

	private GraphicsDebugVisualisation debugVisualisation = GraphicsDebugVisualisation.None;

	/// <summary>Optional visualisation to be used when rendering the scene, instead of rendering it normally (<see cref="GraphicsDebugVisualisation.None"/>)</summary>
	public GraphicsDebugVisualisation DebugVisualisation
	{
		get => debugVisualisation;
		set
		{
			if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException<GraphicsDebugVisualisation>(value, "Invalid debug visualisation", nameof(DebugVisualisation), Enum.GetValues<GraphicsDebugVisualisation>());
			debugVisualisation = value;
		}
	}

	private int lightSampleCountHint = 2;

	/// <summary>Hint to materials as to how many times they should sample each light (as opposed to 1 sample per light per intersection).</summary>
	public int LightSampleCountHint
	{
		get => lightSampleCountHint;
		set
		{
			if (value < 1) throw new ArgumentOutOfRangeException<int>(value, "Must have at least 1 sample per light", nameof(LightSampleCountHint), (1,int.MaxValue));
			lightSampleCountHint = value;
		}
	}
}