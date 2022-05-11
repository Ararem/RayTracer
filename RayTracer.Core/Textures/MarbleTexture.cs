using RayTracer.Core.Hittables;
using SharpNoise.Modules;
using static System.MathF;

namespace RayTracer.Core.Textures;

public record MarbleTexture : Texture
{
	//TODO: Properties, comments, docs & more options to modify
	private float Scale         => 1f;
	private float NoiseScale    => 0.5f;
	private float NoiseStrength => 1f;

	private Module noise =>
			new Perlin
			{
					// Quality   = NoiseQuality.Best,
					Persistence = .5,
					Lacunarity  = 3,
					OctaveCount = 15
			};

	/// <inheritdoc/>
	public override Colour GetColour(HitRecord hit)
	{
		float x = hit.WorldPoint.X / Scale, y = hit.WorldPoint.Y / Scale, z = hit.WorldPoint.Z / Scale;
		float t = x;
		t += (float)noise.GetValue(x / NoiseScale, y / NoiseScale, z / NoiseScale) * NoiseStrength;
		t =  t                                                                     * 2 * PI; //Adjust for 1 wave per cycle

		t = Sin(t);
		t = (0.5f * t) + 0.5f; //Remap [-1..1] to [0..1]
		t = Pow(t, 1 / 5f);    //Make the curve rise rapidly (bias towards 1)
		// t = .2f + 0.75f * t; // Remap from [0..1] to [0.2 .. 0.95]
		Colour col = new(t);
		col = new Colour(col.R, col.G, col.B * 0.95f); //Make the colour slightly warmer
		return col;
	}
}