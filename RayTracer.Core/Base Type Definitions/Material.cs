using JetBrains.Annotations;
using System.Buffers;

namespace RayTracer.Core;

/// <summary>A class that defines a material that a <see cref="Hittable"/> can have</summary>
[PublicAPI]
public abstract class Material : RenderAccessor
{
	/// <summary>Scatters an input ray, according to this material's properties</summary>
	/// <param name="hit">Information such as where the ray hit the object, surface normals, etc</param>
	/// <param name="previousHits">Collection of the previous hits between the camera and the current hit</param>
	/// <returns>
	///  An <see cref="ArraySegment{T}"/> containing the new rays, each of which represents the direction a light ray would be scattered in when bouncing off
	///  this material's surface.
	///  <para>
	///   <b>
	///    <i>
	///     IMPORTANT: The array contained in the array segment that is returned WILL BE RETURNED TO THE ARRAY POOL after it is processed. You must only ever
	///     use the <see cref="ArrayPool{T}"/> to create the arrays for this. And never expose them elsewhere, or this can cause serious bugs.
	///    </i>
	///   </b>
	///  </para>
	/// </returns>
	/// <remarks>
	///  For a completely reflective material, the resulting ray would be 'flipped' around the surface <see cref="HitRecord.Normal"/>, and for a completely
	///  diffuse object, it would be in a random direction. If the resulting <see cref="ArraySegment{T}"/> has a length of <c>0</c> (or is
	///  <see cref="ArraySegment{T}.Empty"/>), it is inferred that the material absorbed the incoming ray.
	/// </remarks>
	public abstract ArraySegment<Ray> Scatter(HitRecord hit, ArraySegment<HitRecord> previousHits);

	/// <summary>Function to override for when the material wants to do lighting calculations, based on the light from future rays</summary>
	/// <param name="previousRayColours">
	///  The colour information for the future bounces that were made. Modify this to vary how your material behaves colour-wise/lighting-wise.
	///  This is the colour from
	/// </param>
	/// <param name="hit">Information such as where the ray hit, surface normals etc</param>
	/// <param name="previousHits">Collection of the previous hits between the camera and the current hit</param>
	/// <example>
	///  <para>
	///   <b>
	///    <i>Note that only the method body is shown here. Some of the names may have changed, however the general idea remains the same</i>
	///   </b>
	///  </para>
	///  <para>
	///   A simple implementation that multiplies by the colour red, treating the object as completely red:
	///   <code>
	///  	return previousRayColours[0] * Colour.Red;
	///  </code>
	///  </para>
	///  <para>
	///   A simple implementation that adds blue light, simulating a blue light-emitting light-source:
	///   <code>
	///  	return previousRayColours[0] + Colour.Blue;
	///  </code>
	///  </para>
	///  <para>
	///   A simple implementation that adds half-white light and multiplies red, simulating a dim white light-emitting object that reflects red light
	///   <code>
	/// 	//Only 30% white is added so it's not too bright, but all red is reflected
	///  	return  (previousRayColours[0] * Colour.Red) + (Colour.White * 0.3f);
	///  </code>
	///  </para>
	/// </example>
	public abstract Colour CalculateColour(ArraySegment<Colour> previousRayColours, HitRecord hit, ArraySegment<HitRecord> previousHits);

	/// <summary>
	///  Simple helper method that calculates the light colour by summing the colour from all lights in the scene. Use this unless you want special light
	///  handling (e.g. specular highlights)
	/// </summary>
	public Colour CalculateSimpleColourFromLights(HitRecord hit)
	{
		Colour sum = Colour.Black;
		for (int i = 0; i < Renderer.Scene.Lights.Length; i++)
		{
			sum += Renderer.Scene.Lights[i].CalculateLight(hit, out _);
		}

		return sum;
	}
}