using RayTracer.Core;
using SharpNoise.Modules;

namespace RayTracer.Impl.Textures;

/// <summary>A texture that outputs a greyscale colour depending on the noise value of the noise generator <see cref="Module"/></summary>
/// <param name="Module">Noise generator module used to generate the colour values</param>
public record GreyscaleNoiseTexture(Module Module) : Texture
{
	/// <inheritdoc/>
	public override Colour GetColour(HitRecord hit) => new((float)Module.GetValue(hit.WorldPoint.X, hit.WorldPoint.Y, hit.WorldPoint.Z));
}