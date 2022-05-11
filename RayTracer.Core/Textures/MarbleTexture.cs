using RayTracer.Core.Hittables;
using SharpNoise;
using SharpNoise.Modules;
using static System.MathF;

namespace RayTracer.Core.Textures;

/// <summary>
/// Texture that mimics the appearance of Marble.
/// </summary>
/// <param name="Scale">How scaled the texture is. Higher values increase the 'zoom', while lower values increase it</param>
/// <param name="NoiseScale">Same as <see cref="Scale"/>, but only affects the noise pattern</param>
/// <param name="NoiseStrength">How much the noise contributes to the output colour</param>
public sealed record MarbleTexture(float Scale = .15f, float NoiseScale = 6f, float NoiseStrength = 3f) : Texture
{
	/// <summary>
	/// Power controlling how fast the value drops off
	/// </summary>
	private const float DropoffPower = 1 / 6f;
	private static readonly Module Noise =
			new Perlin
			{
					Quality   = NoiseQuality.Fast,
					Persistence = .5,
					Lacunarity  = 3,
					OctaveCount = 5
			};

	/// <summary>The texture used for the dark regions of marble (the accent)</summary>
	public Colour AccentColour { get; init; } = new Colour(0,0,0);

	/// <summary>The texture used for the 'light' sections of marble</summary>
	public Colour BaseColour { get; init; } = new Colour(1,1,.95f);

	/// <inheritdoc/>
	public override Colour GetColour(HitRecord hit)
	{
		float x = hit.WorldPoint.X / Scale, y = hit.WorldPoint.Y / Scale, z = hit.WorldPoint.Z / Scale;
		float t = x + y + z;
		t += (float)Noise.GetValue(x / NoiseScale, y / NoiseScale, z / NoiseScale) * NoiseStrength;

		float val = Sin(t);
		val = (0.5f * val) + 0.5f; //Remap [-1..1] to [0..1]
		val = Pow(val, DropoffPower);    //Make the curve rise rapidly (bias towards 1)
		return Colour.Lerp(AccentColour, BaseColour, val);
	}
}
/// <summary>
/// Texture that mimics the appearance of Dark Marble.
/// </summary>
/// <param name="Scale">How scaled the texture is. Higher values increase the 'zoom', while lower values increase it</param>
/// <param name="NoiseScale">Same as <see cref="Scale"/>, but only affects the noise pattern</param>
/// <param name="NoiseStrength">How much the noise contributes to the output colour</param>
public sealed record DarkMarbleTexture() : Texture
{
	private float Scale         => .15f;
	private float NoiseScale    => 6f;
	private float NoiseStrength => 3f;
	/// <summary>The texture used for the dark regions of marble (the accent)</summary>
	public Colour AccentColour => new (1,1,1);

	/// <summary>The texture used for the 'light' sections of marble</summary>
	public Colour BaseColour => new (0.01f);

	/// <summary>
	/// Power controlling how fast the value drops off
	/// </summary>
	private float DropoffPower => 1/8f;
	private static readonly Module Noise = new Perlin
			{
					Quality   = NoiseQuality.Fast,
					Persistence = .5,
					Lacunarity  = 3,
					OctaveCount = 5
			};

	/// <inheritdoc/>
	public override Colour GetColour(HitRecord hit)
	{
		float x = hit.WorldPoint.X / Scale, y = hit.WorldPoint.Y / Scale, z = hit.WorldPoint.Z / Scale;
		float t = x + y + z;
		t += (float)Noise.GetValue(x / NoiseScale, y / NoiseScale, z / NoiseScale) * NoiseStrength;

		float val = Sin(t);
		val =  (0.5f * val) + 0.5f; //Remap [-1..1] to [0..1]
		val *= 20;
		val =  Math.Clamp(val, 0, 1);
		val =  Pow(Abs(val), DropoffPower) * Sign(val); //Make the curve rise rapidly (bias towards +-1)
		return Colour.Lerp(BaseColour, AccentColour,1-val);
	}
}