using RayTracer.Core.Graphics;

namespace RayTracer.Core.Environment;

/// <summary>
///  Simple default skybox that creates a blue-white gradient according to a ray's direction's Y value
/// </summary>
public sealed record DefaultSkyBox : SkyBox
{
	/// <inheritdoc/>
	public override Colour GetSkyColour(Ray ray)
	{
		float t = 0.5f * (ray.Direction.Y + 1);
		return new Colour((1              - t) + (0.5f * t), (1 - t) + (0.7f * t), (1 - t) + (1f * t));
	}
}