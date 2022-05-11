using RayTracer.Core.Hittables;
using SharpNoise;
using SharpNoise.Modules;
using static System.MathF;

namespace RayTracer.Core.Textures;

public record MarbleTexture(Colour Dark, Colour Light, float Scale = 1f, float NoiseScale = 1f, float NoiseStrength = 5f) : Texture
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

	/// <inheritdoc/>
	public override Colour GetColour(HitRecord hit)
	{
		float x = hit.WorldPoint.X / Scale, y = hit.WorldPoint.Y / Scale, z = hit.WorldPoint.Z / Scale;
		float t = x + y + z;
		t += (float)Noise.GetValue(x / NoiseScale, y / NoiseScale, z / NoiseScale) * NoiseStrength;

		float val = Sin(t);
		val = (0.5f * val) + 0.5f; //Remap [-1..1] to [0..1]
		val = Pow(val, DropoffPower);    //Make the curve rise rapidly (bias towards 1)
		// Colour col = new(val);
		// col = new Colour(col.R, col.G, col.B * 0.95f); //Make the colour slightly warmer
		return Colour.Lerp(Dark, Light, val);
	}
}