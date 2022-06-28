using RayTracer.Core;
using System.Numerics;
using static System.Numerics.Vector3;
using static System.MathF;
using Log = Serilog.Log;

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
	public override Ray? Scatter(HitRecord hit, ArraySegment<HitRecord> previousHits) => null;

	/// <inheritdoc/>
	public override Colour CalculateColour(Colour previousColour, HitRecord hit, ArraySegment<HitRecord> previousHits)
	{
		//TODO: Proper reflections and scattering
		//TODO: Material colour
		Colour diffuseColourSum  = Colour.Black;
		Colour specularColourSum = Colour.Black;

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
		 	specularColourSum += specularLightColour * SpecularColour * Pow(specDotProd, Shininess);

		 	//Diffuse if calculated by how much the direction to the light source aligns with the surface normal
		 	float diffDotProd = Dot(diffuseRayTowardsLight.Direction, hit.Normal);
		 	diffDotProd   =  Abs(diffDotProd); //Don't allow for negative dot products
		 	diffuseColourSum += diffuseLightColour * DiffuseColour * diffDotProd;
		 }

		return AmbientColour + diffuseColourSum + specularColourSum;
	}
}