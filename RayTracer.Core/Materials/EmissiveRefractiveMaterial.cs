using JetBrains.Annotations;
using RayTracer.Core.Hittables;
using RayTracer.Core.Textures;
using System.Diagnostics;
using System.Numerics;
using static RayTracer.Core.RandUtils;
using static System.Numerics.Vector3;
using static System.MathF;

namespace RayTracer.Core.Materials;

/// <summary>
///  A material (such as glass) that refracts light rays going through it. This is an extended version of the <see cref="RefractiveMaterial"/>, as it
///  supports emissive textures as well
/// </summary>
/// <param name="RefractiveIndex">Refractive index of the material to simulate</param>
/// <param name="Tint">Texture to tint the rays by</param>
/// <param name="Emission">Texture for the colour of the emitted light</param>
/// <param name="IndirectEmissionOnly">
///  Option whether to only emit light indirectly (directly means direct to the camera, indirect means only when reflecting off other surfaces). If
///  <see langword="true"/>, light will only be emitted when a given ray has bounced off another object before.
/// </param>
public sealed record EmissiveRefractiveMaterial(float RefractiveIndex, Texture Tint, Texture Emission, bool IndirectEmissionOnly = true) : Material
{
	/// <summary>
	///  Refractive index of a common material
	/// </summary>
	[PublicAPI] public const float AirIndex = 1f, GlassIndex = 1.5f, DiamondIndex = 2.4f;

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
			if (reflectance > RandomFloat01())
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
	public override void DoColourThings(ref Colour colour, HitRecord hit, ArraySegment<(SceneObject sceneObject, HitRecord hitRecord)> previousHits)
	{
		colour *= Tint.GetColour(hit);
		//Don't care about (in)direct, force emit
		if (IndirectEmissionOnly == false)
			colour += Emission.GetColour(hit);
		//Care whether it's direct or not
		else
			switch (previousHits.Count)
			{
				//Direct ray from camera
				case 0:
				//Semi indirect - refracted through this object once before
				case 1 when previousHits[0].sceneObject.Material == this:
					return;
				//Indirect case
				default:
					colour += Emission.GetColour(hit);
					return;
			}
	}
}