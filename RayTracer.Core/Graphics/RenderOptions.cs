using JetBrains.Annotations;

namespace RayTracer.Core.Graphics;

/// <summary>
///  Record used to configure how a renderer renders a <see cref="Scenes.Scene"/>
/// </summary>
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
/// <param name="ThreadBatching">How many pixels should be 'batched' together when rendering. Low values (less than a few thousand) introduce overhead. Try to keep this a factor of the (<paramref name="Width"/>*<paramref name="Height"/>)</param>
/// <param name="ConcurrencyLevel">Maximum number of threads that can run at a time. Set to `-1` for no limit, or any other positive integer for that many threads (0 is invalid)</param>
/// <param name="Passes">How many samples to average, to create a less noisy image</param>
/// <param name="MaxDepth">The maximum number of times the rays from the camera are allowed to bounce off surfaces</param>
public sealed record RenderOptions(
		[NonNegativeValue] int     Width,
		[NonNegativeValue] int     Height,
		[NonNegativeValue] float   KMin,
		[NonNegativeValue] float   KMax,
		[NonNegativeValue] int     ThreadBatching,
		[NonNegativeValue] int     ConcurrencyLevel,
		[NonNegativeValue] int     Passes,
		[NonNegativeValue] int     MaxDepth,
		GraphicsDebugVisualisation DebugVisualisation = GraphicsDebugVisualisation.None
)
{
	/// <summary>
	///  Default render options
	/// </summary>
	public static readonly RenderOptions Default = new(1920, 1080, 0.001f, float.PositiveInfinity, 65536, -1, 100, 100);
}