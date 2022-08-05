using Ararem.RayTracer.Core;
using SharpNoise;
using SharpNoise.Modules;
using static System.MathF;

namespace Ararem.RayTracer.Impl.Textures;

/// <summary>Texture that mimics the appearance of Marble.</summary>
public sealed class MarbleTexture : Texture
{
	private static readonly Module Noise = new Perlin
	{
			Quality     = NoiseQuality.Fast,
			Persistence = .5,
			Lacunarity  = 3,
			OctaveCount = 5
	};

	/// <summary>Texture that mimics the appearance of Marble.</summary>
	/// <param name="scale">How scaled the texture is. Higher values increase the 'zoom', while lower values increase it</param>
	/// <param name="noiseScale">Same as <see cref="Scale"/>, but only affects the noise pattern</param>
	/// <param name="noiseStrength">How much the noise contributes to the output colour</param>
	/// <param name="dropoffPower">
	///  Value that controls how much values are biased. Should be &lt;1. Noise values are raised to this power before being processed into a colour.
	///  Increasing this increases the 'sharpness' of the texture and how thin the accent lines are
	/// </param>
	public MarbleTexture(float scale = .15f, float noiseScale = 6f, float noiseStrength = 3f, float dropoffPower = 1 / 6f)
	{
		Scale         = scale;
		NoiseScale    = noiseScale;
		NoiseStrength = noiseStrength;
		DropoffPower  = dropoffPower;
	}

	/// <summary>The texture used for the dark regions of marble (the accent)</summary>
	public Colour AccentColour { get; } = new(0, 0, 0);

	/// <summary>The texture used for the 'light' sections of marble. Defaults to a slightly warm white</summary>
	public Colour BaseColour { get; } = new(1, 1, .95f);

	/// <summary>How scaled the texture is. Higher values increase the 'zoom', while lower values increase it</summary>
	public float Scale { get; }

	/// <summary>Same as <see cref="Scale"/>, but only affects the noise pattern</summary>
	public float NoiseScale { get; }

	/// <summary>How much the noise contributes to the output colour</summary>
	public float NoiseStrength { get; }

	/// <summary>
	///  Value that controls how much values are biased. Should be &lt;1. Noise values are raised to this power before being processed into a colour.
	///  Increasing this increases the 'sharpness' of the texture and how thin the accent lines are
	/// </summary>
	public float DropoffPower { get; }

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