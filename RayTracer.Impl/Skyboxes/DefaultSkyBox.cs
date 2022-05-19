using JetBrains.Annotations;
using RayTracer.Core;

namespace RayTracer.Impl.Skyboxes;

/// <summary>Simple default skybox that creates a blue-white gradient according to a ray's direction's Y value</summary>
[PublicAPI]
public sealed class DefaultSkyBox : SkyBox
{
	/// <inheritdoc/>
	[Pure]
	public override Colour GetSkyColour(Ray ray)
	{
		float t = 0.5f * (ray.Direction.Y + 1);
		return new Colour((1              - t) + (0.5f * t), (1 - t) + (0.7f * t), (1 - t) + (1f * t));
	}
}