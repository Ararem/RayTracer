using JetBrains.Annotations;
using RayTracer.Core.Debugging;

namespace RayTracer.Core;

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
/// <param name="MaxDepth">The maximum number of times the rays from the camera are allowed to bounce off surfaces</param>
/// <param name="ScatterCountHint">A hint parameter that can be accessed by a <see cref="Material"/> for how many times a material should scatter</param>
/// <param name="ScatterCountHint">A hint parameter that can be accessed by a <see cref="Material"/> for how many times it should cast light rays</param>
public sealed record RenderOptions(
		[NonNegativeValue] int     Width,
		[NonNegativeValue] int     Height,
		[NonNegativeValue] float   KMin,
		[NonNegativeValue] float   KMax,
		int                        ConcurrencyLevel,
		int                        Passes,
		[NonNegativeValue] int     MaxDepth,
		GraphicsDebugVisualisation DebugVisualisation = GraphicsDebugVisualisation.None,
		[NonNegativeValue] int ScatterCountHint = 1,
		[NonNegativeValue] int LightSampleCountHint = 1
		//TODO: Max depth for calculating lighting?
)
{
	/// <summary>Default render options</summary>
	public static readonly RenderOptions Default = new(1920, 1080, 0.001f, float.PositiveInfinity, -1, 100, 10);
}