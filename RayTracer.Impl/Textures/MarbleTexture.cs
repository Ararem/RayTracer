using RayTracer.Core;
using SharpNoise;
using SharpNoise.Modules;
using static System.MathF;

namespace RayTracer.Impl.Textures;

/// <summary>Texture that mimics the appearance of Marble.</summary>
/// <param name="Scale">How scaled the texture is. Higher values increase the 'zoom', while lower values increase it</param>
/// <param name="NoiseScale">Same as <see cref="Scale"/>, but only affects the noise pattern</param>
/// <param name="NoiseStrength">How much the noise contributes to the output colour</param>
/// <param name="DropoffPower">
///  Value that controls how much values are biased. Should be &lt;1. Noise values are raised to this power before being processed into a colour.
///  Increasing this increases the 'sharpness' of the texture and how thin the accent lines are
/// </param>
public sealed record MarbleTexture(float Scale = .15f, float NoiseScale = 6f, float NoiseStrength = 3f, float DropoffPower = 1 / 6f) : Texture
{
	private static readonly Module Noise =
			new Perlin
			{
					Quality     = NoiseQuality.Fast,
					Persistence = .5,
					Lacunarity  = 3,
					OctaveCount = 5
			};

	/// <summary>The texture used for the dark regions of marble (the accent)</summary>
	public Colour AccentColour { get; init; } = new(0, 0, 0);

	/// <summary>The texture used for the 'light' sections of marble. Defaults to a slightly warm white</summary>
	public Colour BaseColour { get; init; } = new(1, 1, .95f);

	/// <inheritdoc/>
	public override Colour GetColour(HitRecord hit)
	{
		float x = hit.WorldPoint.X / Scale, y = hit.WorldPoint.Y / Scale, z = hit.WorldPoint.Z / Scale;
		float t = x + y + z; //Sum X, Y and Z to ensure that there is some pattern even across flat planes
		t += (float)Noise.GetValue(x / NoiseScale, y / NoiseScale, z / NoiseScale) * NoiseStrength;

		float val = Sin(t);
		val = (0.5f * val) + 0.5f;    //Remap [-1..1] to [0..1]
		val = Pow(val, DropoffPower); //Make the curve rise rapidly (bias towards 1)
		return Colour.Lerp(AccentColour, BaseColour, val);
	}
}