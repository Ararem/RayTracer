using Ararem.RayTracer.Core;
using Ararem.RayTracer.Impl.Textures;
using System.Numerics;
using static Ararem.RayTracer.Core.RandUtils;
using static System.Numerics.Vector3;

namespace Ararem.RayTracer.Impl.Materials;

/// <summary>A standard material that can be used for both reflective and diffuse surfaces</summary>
public sealed class StandardMaterial : Material
{
	/// <summary>Creates a new standard material with <paramref name="albedo"/> and <paramref name="emission"/> textures</summary>
	/// <param name="albedo">The albedo (colour) texture of this material</param>
	/// <param name="emission">The texture used for the light this material emits</param>
	/// <param name="diffusion">How 'diffuse' (random) the reflected rays are. Settings this to 0 means perfect reflections, 1 means completely diffuse</param>
	public StandardMaterial(Texture albedo, Texture emission, float diffusion)
	{
		Albedo    = albedo;
		Emission  = emission;
		Diffusion = diffusion;
	}

	/// <summary>Creates a new standard material with  <paramref name="albedo"/> and <paramref name="emission"/> colours</summary>
	/// <param name="albedo">The albedo (colour) texture of this material</param>
	/// <param name="emission">The texture used for the light this material emits</param>
	/// <param name="diffusion">How 'diffuse' (random) the reflected rays are. Settings this to 0 means perfect reflections, 1 means completely diffuse</param>
	public StandardMaterial(Colour albedo, Colour emission, float diffusion)
	{
		Albedo    = new SolidColourTexture(albedo);
		Emission  = new SolidColourTexture(emission);
		Diffusion = diffusion;
	}

	/// <summary>Creates a new standard material with only an <paramref name="albedo"/> texture</summary>
	/// <param name="albedo">The albedo (colour) texture of this material</param>
	/// <param name="diffusion">How 'diffuse' (random) the reflected rays are. Settings this to 0 means perfect reflections, 1 means completely diffuse</param>
	public StandardMaterial(Texture albedo, float diffusion)
	{
		Albedo    = albedo;
		Emission  = new SolidColourTexture(Colour.Black);
		Diffusion = diffusion;
	}

	/// <summary>Creates a new standard material with only an <paramref name="albedo"/> colour</summary>
	/// <param name="albedo">The albedo (colour) texture of this material</param>
	/// <param name="diffusion">How 'diffuse' (random) the reflected rays are. Settings this to 0 means perfect reflections, 1 means completely diffuse</param>
	public StandardMaterial(Colour albedo, float diffusion)
	{
		Albedo    = new SolidColourTexture(albedo);
		Emission  = new SolidColourTexture(Colour.Black);
		Diffusion = diffusion;
	}

	/// <summary>The albedo (colour) texture of this material</summary>
	public Texture Albedo { get; }

	/// <summary>The texture used for the light this material emits</summary>
	public Texture Emission { get; }

	/// <summary>How 'diffuse' (random) the reflected rays are. Settings this to 0 means perfect reflections, 1 means completely diffuse</summary>
	public float Diffusion { get; }

	/// <inheritdoc/>
	public override Ray? Scatter(HitRecord hit, ArraySegment<HitRecord> previousHits)
	{
		Vector3 diffuse                           = RandomOnUnitSphere(); //Pick a random scatter direction
		if (Dot(diffuse, hit.Normal) < 0) diffuse *= -1;                  //Ensure the resulting scatter is in the same direction as the normal (so it doesn't point inside the object)
		Vector3 reflect                           = Reflect(hit.IncomingRay.Direction, hit.Normal);

		return new Ray(hit.WorldPoint, Normalize(Lerp(reflect, diffuse, Diffusion)));
	}

	/// <inheritdoc/>
	public override Colour CalculateColour(Colour futureRayColour, Ray futureRay, HitRecord currentHit, ArraySegment<HitRecord> prevHitsBetweenCamera)
	{
		Colour colour = CalculateSimpleColourFromLights(currentHit) + futureRayColour;
		colour *= Albedo.GetColour(currentHit);
		colour += Emission.GetColour(currentHit);
		return colour;
	}
}