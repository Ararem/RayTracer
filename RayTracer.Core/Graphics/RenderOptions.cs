namespace RayTracer.Core.Graphics;

/// <summary>
/// Record used to configure how the <see cref="Renderer"/> renders a <see cref="Scenes.Scene"/>
/// </summary>
/// <param name="Width">How many pixels wide the image should be</param>
/// <param name="Height">How many pixels high the image should be</param>
/// <param name="KMin">The minimum distance away a point must be from a view ray in order to be considered valid. Acts similar to a traditional camera's near plane, however it is based off distance from the last intersection rather than the camera. If this value is &lt;0, objects behind the camera will be visible, and graphical glitches will occur</param>
/// <param name="KMax">The max distance away a point must be from a view ray in order to be considered valid. Acts similar to a traditional camera's near plane, however it is based off distance from the last intersection rather than the camera. Should be &gt; <paramref name="KMin"/>, it's best to leave it extremely high (e.g. <see cref="float.PositiveInfinity"/>)</param>
/// <param name="DebugNormals">Debug flag for rendering surface normals instead of the material</param>
public sealed record RenderOptions(
		int Width,
		int Height,
		float KMin,
		float KMax,
		bool DebugNormals = true
		);