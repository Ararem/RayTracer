using RayTracer.Core;
using System.Numerics;
using static System.Numerics.Vector3;
using static System.MathF;

namespace RayTracer.Impl.Materials;

public class PhongMaterial : Material
{
	/// <summary>Specular colour reflection constant; the ratio of reflection of the specular term of incoming light,</summary>
	public Colour SpecularColour { get; init; } = Colour.White;

	/// <summary>
	/// Colour of the diffused light
	/// </summary>
	public Colour DiffuseColour { get; init; } = Colour.HalfGrey;

	/// <summary>Ambient colour reflection constant; the ratio of reflection of the ambient term present in all points in the scene rendered</summary>
	public Colour AmbientColour { get; init; } = Colour.White *0.001f;

	/// <summary>
	///  Shininess constant for this material, which is larger for surfaces that are smoother and more mirror-like. When this constant is large the specular
	///  highlight is small.
	/// </summary>
	public float Shininess { get; init; }

	/// <inheritdoc/>
	public override Ray? Scatter(HitRecord hit, ArraySegment<HitRecord> previousHits)
	{
		//TODO: Multiple scatterings?
		//Choose a completely random scatter direction
		Vector3 diffuse                           = RandUtils.RandomOnUnitSphere(); //Pick a random scatter direction
		if (Dot(diffuse, hit.Normal) < 0) diffuse *= -1;                  //Ensure the resulting scatter is in the same direction as the normal (so it doesn't point inside the object)
		Vector3 scatter                           = diffuse;

		// // Catch degenerate scatter direction (when scatter magnitude is almost 0)
		// const float thresh = (float)1e-5;
		// if ((scatter.X < thresh) && (scatter.Y < thresh) && (scatter.Z < thresh))
		// 	scatter = hit.Normal;

		Ray r = new(hit.WorldPoint, Normalize(scatter));
		return r;
	}

	/// <inheritdoc/>
	public override Colour CalculateColour(Colour previousColour, HitRecord hit, ArraySegment<HitRecord> previousHits)
	{
		//TODO: Proper reflections and scattering
		//TODO: Material colour
		Colour rawDiffuseColourSum  = Colour.Black;
		Colour rawSpecularColourSum = Colour.Black;

		//Loop over the lights and calculate the diffuse & specular from them
		 for (int i = 0; i < Renderer.Scene.Lights.Length; i++)
		 {
		 	Light  light       = Renderer.Scene.Lights[i];
		 	Colour diffuseLightColour = light.CalculateLight(hit, out Ray diffuseRayTowardsLight);
		 	Colour specularLightColour = light.CalculateLight(hit, out Ray specularRayTowardsLight, true);

		 	//Specular is calculated by how much the reflected light from the light source points towards the viewer (the intersection ray)
		 	Vector3 reflectedLightDirection = Reflect(specularRayTowardsLight.Direction, -hit.Normal);
		 	float   specDotProd               = Dot(hit.Ray.Direction, reflectedLightDirection);
		 	specDotProd    =  Abs(specDotProd); //Don't allow for negative dot products
		 	rawSpecularColourSum += specularLightColour * Pow(specDotProd, Shininess);

		 	//Diffuse if calculated by how much the direction to the light source aligns with the surface normal
		 	float diffDotProd = Dot(diffuseRayTowardsLight.Direction, hit.Normal);
		 	diffDotProd   =  Abs(diffDotProd); //Don't allow for negative dot products
		 	rawDiffuseColourSum += diffuseLightColour * diffDotProd;
		 }

		//Now we do the same that we just did to the lights, but to the scattered ray
		//Replace the "ray XX towards light" with the ray towards the hit, and the light colour with `previousColour`
		//Also, we don't take into account the dot product for the diffuse calculations, since this just makes everything really dark
		{
			//Specular is calculated by how much the reflected light points towards the viewer (the intersection ray)
			Vector3 reflectedLightDirection = Reflect(hit.Ray.Direction, -hit.Normal);
			float   specDotProd             = Dot(hit.Ray.Direction, reflectedLightDirection);
			specDotProd          =  Abs(specDotProd); //Don't allow for negative dot products
			rawSpecularColourSum += previousColour * Pow(specDotProd, Shininess);

			//Diffuse if calculated by how much the direction to the light source aligns with the surface normal
			rawDiffuseColourSum += previousColour;
		}

		return AmbientColour + (rawDiffuseColourSum * DiffuseColour) + (rawSpecularColourSum * SpecularColour);
	}
}