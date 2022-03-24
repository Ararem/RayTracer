using JetBrains.Annotations;
using RayTracer.Core.Graphics;
using RayTracer.Core.Hittables;
using RayTracer.Core.Textures;
using System.Numerics;
using static RayTracer.Core.Rand;
using static System.Numerics.Vector3;
using static System.MathF;

namespace RayTracer.Core.Materials;

//TODO: Emission?

/// <summary>
/// A material (such as glass) that refracts light rays going through it
/// </summary>
/// <param name="RefractiveIndex">Refractive index of the material to simulate</param>
/// <param name="Tint">Texture to tint the rays by</param>
public sealed record RefractiveMaterial(float RefractiveIndex, Texture Tint) : Material
{
	/// <summary>
	/// Refractive index of a common material
	/// </summary>
	[PublicAPI]
	public const float AirIndex = 1f, GlassIndex = 1.5f, DiamondIndex = 2.4f;

#region Overrides of Material

	/// <inheritdoc/>
	public override Ray? Scatter(HitRecord hit)
	{
		Vector3 unitDirection = Normalize(hit.Ray.Direction);

		float cosTheta = Min(Dot(-unitDirection, hit.Normal), 1.0f);
		float sinTheta = Sqrt(1.0f - (cosTheta * cosTheta));

		//Eta is the refractive index for the first material (that the ray is in before it refracts)
		//Eta Prime is the refractive index of the second material (the one that the ray will be inside once it refracts)

		//If the hit is 'front-facing', we know that it's from the outside of the sphere, so the first material is air
		//Likewise, the reverse is true - if it's not front-facing then it's leaving the sphere so air is second
		//There's also the assumption that air is the default material, but this isn't too important
		float eta, etaPrime;
		if (hit.OutsideFace)
		{
			eta      = AirIndex;
			etaPrime = RefractiveIndex;
		}
		else
		{
			eta      = RefractiveIndex;
			etaPrime = AirIndex;
		}

		float refractionRatio = eta / etaPrime;
		//Due to snell's law, we can't always refract, sometimes we have to reflect (total internal reflection)
		bool cannotRefract = refractionRatio * sinTheta > 1.0f;

		//Schlick's approximation for reflectance, does some weird thing i don't 100% understand
		//It looks good however when you add it in
		{
			float r0 = (eta - etaPrime) / (eta + etaPrime);
			r0 *= r0;
			float reflectance = r0 + ((1 - r0) * Pow(1 - cosTheta, 5));
			if (reflectance > RandomFloat())
				cannotRefract = true;
		}

		Vector3 outDirection;
		if (cannotRefract)
		{
			outDirection = Reflect(unitDirection, hit.Normal);
		}
		else
		{
			//Complex maths, don't ask me what this does or how it works
			Vector3 refractedRayPerpendicular = refractionRatio * (unitDirection + (cosTheta * hit.Normal));
			Vector3 refractedRayParallel =
					-Sqrt(Abs(1.0f - refractedRayPerpendicular.LengthSquared())) * hit.Normal;
			outDirection = refractedRayPerpendicular + refractedRayParallel;
		}

		return new Ray(hit.WorldPoint, outDirection);
	}

	/// <inheritdoc/>
	public override void DoColourThings(ref Colour colour, HitRecord hit)
	{
		colour *= Tint.GetColour(hit);
	}

#endregion
}