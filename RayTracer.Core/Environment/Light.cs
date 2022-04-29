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
public abstract record Light : RenderAccessor
{
	/// <summary>
	///  Calculates the light emitted by the current <see cref="Light"/> instance, for the hit stored in the <paramref name="hit"/>
	/// </summary>
	/// <param name="hit">The information about the hit to calculate the lighting for</param>
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
	public abstract Colour CalculateLight(HitRecord hit);

	/// <summary>
	///  Returns if there is an intersection between a <paramref name="hit"/> and another <paramref name="position"/>. This overload also allows access to the computed
	///  shadow ray
	/// </summary>
	[PublicAPI]
	protected bool CheckIntersection(HitRecord hit, Vector3 position, out Ray shadowRay)
	{
		//Find ray between the hit and the light
		shadowRay = Ray.FromPoints(hit.WorldPoint, position);
		const float kMin = 0.0001f;
		float       kMax = Vector3.Distance(position, hit.WorldPoint);
		return Renderer.AnyIntersectionFast(shadowRay, kMin, kMax);
	}

	/// <summary>
	///  Returns if there is an intersection between a <paramref name="hit"/> and another <paramref name="position"/>
	/// </summary>
	[PublicAPI]
	protected bool CheckIntersection(HitRecord hit, Vector3 position) => CheckIntersection(hit, position, out _);
}