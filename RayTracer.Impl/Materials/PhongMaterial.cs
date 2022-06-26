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
	public Colour AmbientColour { get; init; } = Colour.Black;

	/// <summary>
	///  Shininess constant for this material, which is larger for surfaces that are smoother and more mirror-like. When this constant is large the specular
	///  highlight is small.
	/// </summary>
	public float Shininess { get; init; }

	public static Colour TEMP_AMBIENT_COLOUR => Colour.White;

	/// <inheritdoc/>
	public override Ray? Scatter(HitRecord hit, ArraySegment<HitRecord> previousHits) => null;

	/// <inheritdoc/>
	public override Colour CalculateColour(Colour previousColour, HitRecord hit, ArraySegment<HitRecord> previousHits)
	{
		//TODO: Proper reflections and scattering
		//TODO: Material colour
		Colour ambientColour  = AmbientColour * TEMP_AMBIENT_COLOUR;
		Colour diffuseColour  = Colour.Black;
		Colour specularColour = Colour.Black;

		//Loop over the lights and calculate the diffuse & specular from them
		// for (int i = 0; i < Renderer.Scene.Lights.Length; i++)
		// {
		// 	Light  light       = Renderer.Scene.Lights[i];
		// 	Colour lightColour = light.CalculateLight(hit, out Ray rayTowardsLight);
		//
		// 	//Specular is calculated by how much the reflected light from the light source points towards the viewer (the intersection ray)
		// 	Vector3 reflectedLightDirection = Reflect(rayTowardsLight.Direction, hit.Normal);
		// 	float   specDotProd               = Dot(hit.Ray.Direction, reflectedLightDirection);
		// 	specDotProd    =  Max(specDotProd, 0f); //Don't allow for negative dot products
		// 	specularColour += lightColour * SpecularColour * Pow(specDotProd, Shininess);
		//
		// 	//Diffuse if calculated by how much the direction to the light source aligns with the surface normal
		// 	float diffDotProd = Dot(rayTowardsLight.Direction, hit.Normal);
		// 	diffDotProd   =  Max(diffDotProd, 0f); //Don't allow for negative dot products
		// 	diffuseColour += lightColour * DiffuseColour * diffDotProd;
		// }

		return ambientColour + diffuseColour + specularColour;
	}
}