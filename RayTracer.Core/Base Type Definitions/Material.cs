using JetBrains.Annotations;

namespace RayTracer.Core;

/// <summary>A class that defines a material that a <see cref="Hittable"/> can have</summary>
[PublicAPI]
public abstract class Material : RenderAccessor
{
	/// <summary>Scatters an input ray, according to this material's properties</summary>
	/// <param name="hit">Information such as where the ray hit the object, surface normals, etc</param>
	/// <param name="previousHits">Collection of the previous hits between the camera and the current hit</param>
	/// <returns>A new ray, which represents the direction a light ray would be scattered in when bouncing off this material's surface</returns>
	/// <remarks>
	///  For a completely reflective material, the resulting ray would be 'flipped' around the surface <see cref="HitRecord.Normal"/>, and for a completely
	///  diffuse object, it would be in a random direction
	/// </remarks>
	public abstract Ray? Scatter(HitRecord hit, ArraySegment<HitRecord> previousHits);

	/// <summary>Function to override for when the material wants to do lighting calculations, based on the light from future rays</summary>
	/// <param name="previousRayColour">
	///  The colour information for the future bounces that were made. Modify this to vary how your material behaves
	///  previousRayColour-wise/lighting-wise. This is the colour from
	/// </param>
	/// <param name="hit">Information such as where the ray hit, surface normals etc</param>
	/// <param name="previousHits">Collection of the previous hits between the camera and the current hit</param>
	/// <remarks>
	///  Use the <paramref name="hit"/> to evaluation world information, such as where on a texture map the point corresponds to, and make changes to the
	///  <paramref name="previousRayColour"/> using that information
	/// </remarks>
	/// <example>
	///  <para>
	///   <b>
	///    <i>Note that only the method body is shown here. Some of the names may have changed, however the general idea remains the same</i>
	///   </b>
	///  </para>
	///  <para>
	///   A simple implementation that multiplies by the previousRayColour red, treating the object as completely red:
	///   <code>
	///  	return previousRayColour * Colour.Red;
	///  </code>
	///  </para>
	///  <para>
	///   A simple implementation that adds blue light, simulating a blue light-emitting light-source:
	///   <code>
	///  	return previousRayColour + Colour.Blue;
	///  </code>
	///  </para>
	///  <para>
	///   A simple implementation that adds half-white light and multiplies red, simulating a dim white light-emitting object that reflects red light
	///   <code>
	/// 	//Only 30% white is added so it's not too bright, but all red is reflected
	///  	return  (previousRayColour * Colour.Red) + (Colour.White * 0.3f);
	///  </code>
	///  </para>
	/// </example>
	public abstract Colour CalculateColour(Colour previousRayColour, HitRecord hit, ArraySegment<HitRecord> previousHits);

	public Colour CalculateSimpleColourFromLights(HitRecord hit)
	{
		Colour sum = Colour.Black;


		for (int i = 0; i < Renderer
							.Scene
							.Lights
							.Length; i++)
		{
			sum += Renderer.Scene.Lights[i].CalculateLight(hit);
		}

		return sum;
	}
}