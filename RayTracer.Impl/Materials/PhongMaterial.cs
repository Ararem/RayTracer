using RayTracer.Core;
using System.Numerics;
using static System.Numerics.Vector3;

namespace RayTracer.Impl.Materials;

public class PhongMaterial : Material
{
	/// <summary>Specular reflection constant; the ratio of reflection of the specular term of incoming light,</summary>
	public float Specular { get; init; }

	/// <summary>
	///  Diffuse reflection constant; the ratio of reflection of the diffuse term of incoming light (
	///  <a href="https://en.m.wikipedia.org/wiki/Lambertian_reflectance">Lambertian reflectance</a>)
	/// </summary>
	public float Diffuse { get; init; }

	/// <summary>Ambient reflection constant; the ratio of reflection of the ambient term present in all points in the scene rendered</summary>
	public float Ambient { get; init; }

	/// <summary>
	///  Shininess constant for this material, which is larger for surfaces that are smoother and more mirror-like. When this constant is large the specular
	///  highlight is small.
	/// </summary>
	public float Shininess { get; init; }

	public static Colour TEMP_AMBIENT_COLOUR => Colour.White;

	/// <inheritdoc/>
	public override Ray? Scatter(HitRecord hit, ArraySegment<HitRecord> previousHits) => TODO_IMPLEMENT_ME;

	/// <inheritdoc/>
	public override Colour CalculateColour(HitRecord hit, ArraySegment<HitRecord> previousHits, IList<Light> lights)
	{
		Colour ambientColour = (Ambient * TEMP_AMBIENT_COLOUR);
		Colour diffuseColour = Colour.Black;
		Colour specularColour = Colour.Black;
		//Loop over the lights
		for (int i = 0; i < lights.Count; i++)
		{
			Vector3 directionTowardsLight          = .Direction;
			Vector3 reflectedDirectionTowardsLight = Reflect(directionTowardsLight, hit.Normal);
			diffuseColour += (Diffuse  * Dot(hit.Normal,                     directionTowardsLight)) * ;
			float   specularAmount                 = (Specular * Dot(reflectedDirectionTowardsLight, -hit.Ray.Direction)); //Flip ray direction because we want away from the hit
		}
	}
}