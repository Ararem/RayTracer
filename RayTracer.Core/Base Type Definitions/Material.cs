using JetBrains.Annotations;
using System.Buffers;

namespace RayTracer.Core;

/// <summary>A class that defines a material that a <see cref="Hittable"/> can have</summary>
[PublicAPI]
public abstract class Material : RenderAccessor
{
	/// <summary>Scatters an input ray, according to this material's properties</summary>
	/// <param name="currentHit">Information such as where the ray currentHit the object, surface normals, etc</param>
	/// <param name="prevHitsBetweenCamera">Collection of the previous hits between the camera and the current currentHit</param>
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
	public abstract ArraySegment<Ray> Scatter(HitRecord currentHit, ArraySegment<HitRecord> prevHitsBetweenCamera);

	/// <summary>Function to override for when the material wants to do lighting calculations, based on the light from future rays</summary>
	/// <param name="futureRayInfo">
	///  The colour and surface information for the future bounces that were made. This only includes the (direct) hits, i.e. those that were as a result of the intersection along the rays returned byt <see cref="Scatter"/>.
	/// </param>
	/// <param name="currentHit">Information such as where the ray currentHit, surface normals etc</param>
	/// <param name="prevHitsBetweenCamera">Collection of the previous hits between the camera and the current currentHit</param>
	public abstract Colour CalculateColour(ArraySegment<(Colour Colour, Ray Ray)> futureRayInfo, HitRecord currentHit, ArraySegment<HitRecord> prevHitsBetweenCamera);

	/// <summary>
	///  Simple helper method that calculates the light colour by summing the colour from all lights in the scene. Use this unless you want special light
	///  handling (e.g. specular highlights)
	/// </summary>
	public Colour CalculateSimpleColourFromLights(HitRecord hit)
	{
		Colour sum     = Colour.Black;
		int    samples = Renderer.RenderOptions.LightSampleCountHint;
		for (int s = 0; s < samples; s++)
		{
			for (int i = 0; i < Renderer.Scene.Lights.Length; i++)
			{
				sum += Renderer.Scene.Lights[i].CalculateLight(hit, out _);
			}
		}

		sum /= samples;

		return sum;
	}
}