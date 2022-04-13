using RayTracer.Core.Hittables;

namespace RayTracer.Core.Environment;

/// <summary>
/// Base class that defines a light that can be used to calculate lighting more accurately in the scene
/// </summary>
public abstract record Light
{

	/// <summary>
	/// Calculates the light emitted by the current <see cref="Light"/> instance, for the hit stored in the <paramref name="hit"/>
	/// </summary>
	/// <param name="hit">The information about the hit to calculate the lighting for</param>
	/// <param name="findClosestIntersection">A function that can be used to calculate the closest intersection along a given ray. Returns the closest intersection and the object intersected with when possible, or <see langword="unll"/> when no intersection along the ray was found (with regards to <see cref="RenderOptions"/>.<see cref="RenderOptions.KMin"/> and <see cref="RenderOptions"/>.<see cref="RenderOptions.KMax"/></param>
	/// <returns>The amount of light received by the <paramref name="hit"/>, from this light source</returns>
	/// <remarks>Use <see cref="findClosestIntersection"/> to detect whether there are objects in between the target object, and the light source.</remarks>
	public abstract Colour CalculateLight(HitRecord hit, Func<Ray, (SceneObject sceneObject, HitRecord hit)?> findClosestIntersection);
}