using RayTracer.Core.Debugging;

namespace RayTracer.Core;

/// <summary>Class that contains properties that are used to control how a <see cref="Scene"/> is rendered</summary>
public sealed class RenderOptions
{
	/// <summary>How many pixels wide the render will be</summary>
	public int RenderWidth { get; init; } = 1920;

	/// <summary>How many pixels high the render will be</summary>
	public int RenderHeight { get; init; } = 1080;

	/// <summary>Minimum K value for intersections to be considered valid (Near plane)</summary>
	/// <remarks>
	///  <c>K</c> values mean the distance along a given ray at which the intersection occured. This essentially means what is the closest hit that we
	///  consider valid. Set this to slightly above 0 to avoid self-intersecting problems, such as shadow acne
	/// </remarks>
	public float KMin { get; set; } = 0.001f;

	/// <summary>Maximum K values for intersections to be considered valid (Far plane)</summary>
	/// <remarks>
	///  <c>K</c> values mean the distance along a given ray at which the intersection occured. This essentially means what is the furthest hit that we
	///  consider valid.
	/// </remarks>
	public float KMax { get; set; } = float.PositiveInfinity;

	/// <summary>Maximum number of threads that can render concurrently</summary>
	/// <remarks>Defaults to the number of (hyper-threaded) cores present (see <see cref="Environment.ProcessorCount"/>)</remarks>
	public int ConcurrencyLevel { get; set; } = Environment.ProcessorCount;

	/// <summary>Number of times the image will be rendered. These individual renders ('passes') will be combined to create the final image</summary>
	public int Passes { get; set; } = 100;

	/// <summary>
	///  Maximum number of bounces allowed before a given ray path is discarded. Essentially puts a limit on how many times a ray can bounce, to avoid
	///  infinite bouncing
	/// </summary>
	public int MaxBounceDepth { get; set; } = 100;

	/// <summary>Optional visualisation to be used when rendering the scene, instead of rendering it normally (<see cref="GraphicsDebugVisualisation.None"/>)</summary>
	public GraphicsDebugVisualisation DebugVisualisation { get; set; } = GraphicsDebugVisualisation.None;

	/// <summary>Hint to materials as to how many times they should sample each light (as opposed to 1 sample per light per intersection).</summary>
	public int LightSampleCountHint { get; set; } = 2;

	public static RenderOptions GetDefault() => new();
}

/// <summary>Record used to configure how a renderer renders a <see cref="Scene"/></summary>
/// <param name="Width">How many pixels wide the image should be</param>
/// <param name="Height">How many pixels high the image should be</param>
/// <param name="KMin">
///  The minimum distance away a point must be from a view ray in order to be considered valid. Acts similar to a traditional camera's near plane,
///  however it is based off distance from the last intersection rather than the camera. If this value is &lt;0, objects behind the camera will be
///  visible, and graphical glitches will occur
/// </param>
/// <param name="KMax">
///  The max distance away a point must be from a view ray in order to be considered valid. Acts similar to a traditional camera's near plane, however it
///  is based off distance from the last intersection rather than the camera. Should be &gt; <paramref name="KMin"/>, it's best to leave it extremely
///  high (e.g. <see cref="float.PositiveInfinity"/>)
/// </param>
/// <param name="DebugVisualisation">Flag for enabling debugging related options, such as showing surface normals</param>
/// <param name="ConcurrencyLevel">
///  Maximum number of threads that can run at a time. Set to `-1` for no limit, or any other positive integer for that
///  many threads (0 is invalid)
/// </param>
/// <param name="Passes">
///  How many samples to average, to create a less noisy image. If equal to <c>-1</c>, the renderer will render infinitely until
///  manually stopped
/// </param>
/// <param name="MaxBounceDepth">The maximum number of times the rays from the camera are allowed to bounce off surfaces</param>
/// <param name="LightSampleCountHint">A hint parameter that can be accessed by a <see cref="Material"/> for how many times it should cast light rays</param>