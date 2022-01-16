using JetBrains.Annotations;
using RayTracer.Core.Scenes;

namespace RayTracer.Core.Graphics;

/// <summary>
/// Represents a camera that is used to render a <see cref="Scene"/>
/// </summary>
/// <remarks>
///	This class handles the creation of view rays for each pixel, which the <see cref="Renderer"/> then uses to create the scene image
/// </remarks>
public sealed record Camera(
		[ValueRange(0, int.MaxValue)] int Width = 1920,
		[ValueRange(0, int.MaxValue)] int Height = 1080,
		[ValueRange(0, int.MaxValue)] int Samples = 100
)
{
	
}