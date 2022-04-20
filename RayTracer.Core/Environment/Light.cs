using JetBrains.Annotations;
using RayTracer.Core.Hittables;
using System.Numerics;

namespace RayTracer.Core.Environment;

/// <summary>
///  Base class that defines a light that can be used to calculate lighting more accurately in the scene
/// </summary>
/// <remarks>
///
/// </remarks>
public abstract record Light
{
	/// <summary>
	///  Calculates the light emitted by the current <see cref="Light"/> instance, for the hit stored in the <paramref name="hit"/>
	/// </summary>
	/// <param name="hit">The information about the hit to calculate the lighting for</param>
	/// <param name="renderer">
	///  The renderer (render job) that is currently rendering using this light. Allows for intersection checks between objects in the scene, in order to calculate shadows.
	/// </param>
	/// <returns>The amount of light received by the <paramref name="hit"/>, from this light source</returns>
	/// <remarks>
	///  When checking for shadowing, you would most likely use <see cref="AsyncRenderJob.AnyIntersectionFast"/> between the point hit and the position of the light
	///  source (see example below)
	/// </remarks>
	/// <example>
	///  Assuming a light has a <c>Vector3 Position</c> property.
	///  <code>
	/// public override Colour CalculateLight(HitRecord hit, AsyncRenderJob renderer)
	/// {
	/// 	Vector3 Position = this.Position;
	/// 	//Find ray between the hit and the light
	/// 	Ray         shadowRay = Ray.FromPoints(hit.WorldPoint, Position);
	/// 	const float kMin      = 0.0001f;
	/// 	float       kMax      = Vector3.Distance(Position, hit.WorldPoint);
	/// 	if (renderer.AnyIntersectionFast(shadowRay, kMin, kMax)) //Shadowed
	/// 		return Colour.Black;
	/// 	else //No shadow
	/// 		return LightColour;
	/// }
	///  </code>
	/// </example>
	/// <seealso cref="SlowClosestIntersectCheck"/>
	/// <seealso cref="FastAnyIntersectCheck"/>
	public abstract Colour CalculateLight(HitRecord hit, AsyncRenderJob renderer);

	/// <summary>
	///  Returns if there is an intersection between a <see cref="hit"/> and another <see cref="position"/>. This overload also allows access to the computed
	///  shadow ray
	/// </summary>
	[PublicAPI]
	protected static bool CheckIntersection(HitRecord hit, Vector3 position, AsyncRenderJob renderer, out Ray shadowRay)
	{
		//Find ray between the hit and the light
		shadowRay = Ray.FromPoints(hit.WorldPoint, position);
		const float kMin = 0.0001f;
		float       kMax = Vector3.Distance(position, hit.WorldPoint);
		return renderer.AnyIntersectionFast(shadowRay, kMin, kMax);
	}

	/// <summary>
	///  Returns if there is an intersection between a <see cref="hit"/> and another <see cref="position"/>
	/// </summary>
	[PublicAPI]
	protected static bool CheckIntersection(HitRecord hit, Vector3 position, AsyncRenderJob renderer) => CheckIntersection(hit, position, renderer, out _);
}