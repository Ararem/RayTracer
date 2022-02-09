using RayTracer.Core.Graphics;
using RayTracer.Core.Hittables;

namespace RayTracer.Core.Materials;

public class ColourMaterial : Material
{
	public ColourMaterial(Colour colour)
	{
		Colour = colour;
	}

	public Colour Colour { get; }

	/// <inheritdoc/>
	public override Ray? Scatter(HitRecord hit) => null;

	/// <inheritdoc/>
	public override void DoColourThings(ref Colour colour, HitRecord hit, int bounces)
	{
		colour = Colour;
	}
}