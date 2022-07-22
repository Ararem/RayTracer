using Ararem.RayTracer.Core;
using JetBrains.Annotations;
using System.Numerics;
using static Ararem.RayTracer.Core.RandUtils;
using static System.Numerics.Vector3;
using static System.MathF;

namespace Ararem.RayTracer.Impl.Materials;

/// <summary>A material (such as glass) that refracts light rays going through it</summary>
public class RefractiveMaterial : Material
{
	/// <summary>Refractive index of a common material</summary>
	[PublicAPI] public const float AirIndex = 1f, GlassIndex = 1.5f, DiamondIndex = 2.4f;

	/// <summary>A material (such as glass) that refracts light rays going through it</summary>
	/// <param name="refractiveIndex">Refractive index of the material to simulate</param>
	/// <param name="tint">Texture to tint the rays by</param>
	/// <param name="alternateRefractionMode">Optional flag that enables an alternate mode of calculating refractions (careful, it's funky)</param>
	public RefractiveMaterial(float refractiveIndex, Texture tint, bool alternateRefractionMode = false)
	{
		RefractiveIndex         = refractiveIndex;
		Tint                    = tint;
		AlternateRefractionMode = alternateRefractionMode;
	}

	/// <summary>Refractive index of the material to simulate</summary>
	public float RefractiveIndex { get; }

	/// <summary>Texture to tint the rays by</summary>
	public Texture Tint { get; }

	/// <summary>Optional flag that enables an alternate mode of calculating refractions</summary>
	public bool AlternateRefractionMode { get; }

	/// <inheritdoc/>
	public override Ray? Scatter(HitRecord hit, ArraySegment<HitRecord> previousHits)
	{
		Vector3 unitDirection = Normalize(hit.IncomingRay.Direction);

		float cosTheta = Min(Dot(-unitDirection, hit.Normal), 1.0f);
		float sinTheta = Sqrt(1.0f - (cosTheta * cosTheta));

		//Eta is the refractive index for the first material (that the ray is in before it refracts)
		//Eta Prime is the refractive index of the second material (the one that the ray will be inside once it refracts)

		float eta, etaPrime;
		//TODO: Make this work by pulling it from other refractive materials?
		//If the ray is going from the outside (air or another material) into the inside (this material),
		//Use the air index as the first refractive index
		bool outsideGoingInside = (previousHits.Count         == 0)     //Direct ray from camera - has to be "from the air"
								|| (previousHits[^1].Material != this); //If previous hit was also this object then we know it's the other way around
		if (outsideGoingInside)
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
			float reflectance                                = r0 + ((1 - r0) * Pow(1 - cosTheta, 5));
			if (reflectance > RandomFloat01()) cannotRefract = true;
		}

		Vector3 outDirection;
		if (cannotRefract)
		{
			outDirection = Reflect(unitDirection, hit.Normal);
		}
		else
		{
			//Big problem, I've got two different ways of calculating the refracted ray and I don't know which way is correct
			//So just let the user decide
			//TODO: Fix refraction

			Vector3 refractedRayPerpendicular = refractionRatio * (unitDirection + (cosTheta * hit.Normal));
			Vector3 refractedRayParallel =
					-Sqrt(Abs(1.0f - refractedRayPerpendicular.LengthSquared())) * hit.Normal;
			Vector3 standard = refractedRayPerpendicular + refractedRayParallel;

			Vector3 alternate = Normalize((Sqrt((1 - Pow(refractionRatio, 2)) * (1 - Pow(Dot(hit.Normal, unitDirection), 2))) * hit.Normal) + (refractionRatio * (unitDirection - (Dot(hit.Normal, unitDirection) * hit.Normal))));

			outDirection = AlternateRefractionMode ? alternate : standard;
		}

		return new Ray(hit.WorldPoint, outDirection);
	}

	/// <inheritdoc/>
	public override Colour CalculateColour(Colour futureRayColour, Ray futureRay, HitRecord currentHit, ArraySegment<HitRecord> prevHitsBetweenCamera) =>
			//TODO: Beer's law and 'slickness'
			(futureRayColour + CalculateSimpleColourFromLights(currentHit)) * Tint.GetColour(currentHit);
}