using JetBrains.Annotations;

namespace Ararem.RayTracer.Core;

/// <summary>A class that defines a material that a <see cref="Hittable"/> can have</summary>
[PublicAPI]
public abstract class Material : RenderAccessor
{
	/// <summary>Scatters an input ray, according to this material's properties</summary>
	/// <param name="currentHit">Information such as where the ray currentHit the object, surface normals, etc</param>
	/// <param name="prevHitsBetweenCamera">Collection of the previous hits between the camera and the current currentHit</param>
	/// <remarks>
	///  For a completely reflective material, the resulting ray would be 'flipped' around the surface <see cref="HitRecord.Normal"/>, and for a completely
	///  diffuse object, it would be in a random direction. If the returned ray is <see langword="null"/>, it is inferred that the ray is absorbed and not
	///  scattered
	/// </remarks>
	public abstract Ray? Scatter(HitRecord currentHit, ArraySegment<HitRecord> prevHitsBetweenCamera);

	/// <summary>Function to override for when the material wants to do lighting calculations, based on the light from future rays</summary>
	/// <param name="futureRayColour">  The colour information for the future bounce that was made.</param>
	/// <param name="futureRay">  The ray for the future bounce that was made.</param>
	/// <param name="currentHit">Information such as where the ray currentHit, surface normals etc</param>
	/// <param name="prevHitsBetweenCamera">Collection of the previous hits between the camera and the current currentHit</param>
	public abstract Colour CalculateColour(Colour futureRayColour, Ray futureRay, HitRecord currentHit, ArraySegment<HitRecord> prevHitsBetweenCamera);

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