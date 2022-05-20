using RayTracer.Core;
using SharpNoise.Modules;

namespace RayTracer.Impl.Textures;

/// <summary>A texture that outputs a greyscale colour depending on the noise value of the noise generator <see cref="Module"/></summary>
public sealed class GreyscaleNoiseTexture : Texture
{
	/// <summary>A texture that outputs a greyscale colour depending on the noise value of the noise generator <see cref="Module"/></summary>
	/// <param name="module">Noise generator module used to generate the colour values</param>
	public GreyscaleNoiseTexture(Module module)
	{
		Module = module;
	}

	/// <summary>Noise generator module used to generate the colour values</summary>
	public Module Module { get; }

	/// <inheritdoc/>
	public override Colour GetColour(HitRecord hit) => new((float)Module.GetValue(hit.WorldPoint.X, hit.WorldPoint.Y, hit.WorldPoint.Z));
}