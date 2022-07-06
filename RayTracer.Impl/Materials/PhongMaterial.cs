using RayTracer.Core;
using System.Numerics;
using static System.Numerics.Vector3;
using static System.MathF;

namespace RayTracer.Impl.Materials;

public class PhongMaterial : Material
{
	private const int SpecularRayIndex = 0, DiffuseRayIndex = 1;

	/// <summary>Specular colour reflection constant; the ratio of reflection of the specular term of incoming light,</summary>
	public Colour SpecularColour { get; init; } = Colour.White;

	/// <summary>Colour of the diffused light</summary>
	public Colour DiffuseColour { get; init; } = Colour.HalfGrey;

	/// <summary>Ambient colour reflection constant; the ratio of reflection of the ambient term present in all points in the scene rendered</summary>
	public Colour AmbientColour { get; init; } = Colour.White * 0.001f;

	/// <summary>
	///  Shininess constant for this material, which is larger for surfaces that are smoother and more mirror-like. When this constant is large the specular
	///  highlight is small.
	/// </summary>
	public float Shininess { get; init; }

	/// <inheritdoc/>
	public override ArraySegment<Ray> Scatter(HitRecord currentHit, ArraySegment<HitRecord> prevHitsBetweenCamera)
	{
		ArraySegment<Ray> seg = ArraySegmentPool.GetPooledSegment<Ray>(2);
		{
			seg[SpecularRayIndex] = new Ray(currentHit.WorldPoint, Reflect(currentHit.IncomingRay.Direction, currentHit.Normal));
		}
		{
			Vector3 dir                              = RandUtils.RandomOnUnitSphere(); //Pick a random scatter direction
			if (Dot(dir, currentHit.Normal) < 0) dir *= -1;                            //Ensure the resulting scatter is in the same direction as the normal (so it doesn't point inside the object)
			seg[DiffuseRayIndex] = new Ray(currentHit.WorldPoint, dir);
		}

		return seg;
	}

	/// <inheritdoc/>
	public override Colour CalculateColour(ArraySegment<(Colour Colour, Ray Ray)> futureRayInfo, HitRecord hit, ArraySegment<HitRecord> prevHitsBetweenCamera)
	{
		Colour rawDiffuseColourSum  = Colour.Black;
		Colour rawSpecularColourSum = Colour.Black;

		//Do a few iterations and average them, to make the colours a bit smoother
		int lightSamples = Renderer.RenderOptions.LightSampleCountHint;
		for (int avgI = 0; avgI < lightSamples; avgI++)
		{
			//Loop over the lights and calculate the diffuse & specular from them
			for (int i = 0; i < Renderer.Scene.Lights.Length; i++)
			{
				Light  light               = Renderer.Scene.Lights[i];
				Colour diffuseLightColour  = light.CalculateLight(hit, out Ray diffuseRayTowardsLight);
				Colour specularLightColour = light.CalculateLight(hit, out Ray specularRayTowardsLight, true);

				//Specular is calculated by how much the reflected light from the light source points towards the viewer (the intersection ray)
				Vector3 reflectedLightDirection = Reflect(specularRayTowardsLight.Direction, -hit.Normal);
				float   specDotProd             = Dot(hit.IncomingRay.Direction, reflectedLightDirection);
				specDotProd = Max(0, specDotProd);
				// specDotProd          =  Abs(specDotProd); //Don't allow for negative dot products
				rawSpecularColourSum += specularLightColour * Pow(specDotProd, Pow(2, Shininess));

				//Diffuse if calculated by how much the direction to the light source aligns with the surface normal
				float diffDotProd = Dot(diffuseRayTowardsLight.Direction, hit.Normal);
				diffDotProd         =  Abs(diffDotProd); //Don't allow for negative dot products
				rawDiffuseColourSum += diffuseLightColour * diffDotProd;
			}
		}

		rawDiffuseColourSum  /= lightSamples;
		rawSpecularColourSum /= lightSamples;

		//Now we do the same that we just did to the lights, but to the scattered rays
		//Replace the "ray XX towards light" with the ray towards the hit, and the light colour with `previousColour`
		//Also, we don't take into account the dot product for the diffuse calculations, since this just makes everything really dark
		{
			(Colour Colour, Ray Ray) specularRay = futureRayInfo[SpecularRayIndex];
			(Colour Colour, Ray Ray) diffuseRay  = futureRayInfo[DiffuseRayIndex];

			Vector3 reflectedLightDirection = Reflect(hit.IncomingRay.Direction, -hit.Normal);
			float   specDotProd             = Dot(hit.IncomingRay.Direction, reflectedLightDirection);
			specDotProd          =  Max(0, specDotProd);
			rawSpecularColourSum += specularRay.Colour * Pow(specDotProd, Pow(2, Shininess));

			rawDiffuseColourSum += diffuseRay.Colour;
		}

		return AmbientColour + (rawDiffuseColourSum * DiffuseColour) + (rawSpecularColourSum * SpecularColour);
	}
}