using JetBrains.Annotations;
using System.Numerics;

namespace RayTracer.Core;

/// <summary>Base class that defines a light that can be used to calculate lighting more accurately in the scene</summary>
[PublicAPI]
public abstract class Light : RenderAccessor
{
	/// <summary>Calculates the light emitted by the current <see cref="Light"/> instance, for the hit stored in the <paramref name="hit"/></summary>
	/// <param name="hit">The information about the hit to calculate the lighting for</param>
	/// <returns>The amount of light received by the <paramref name="hit"/>, from this light source</returns>
	/// <remarks>
	///  When checking for shadowing, you would most likely use <see cref="AsyncRenderJob.AnyIntersectionFast"/> between the point hit and the position of
	///  the light source (see example below)
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
	public Colour CalculateLight(HitRecord hit) => CalculateLight(hit, out _);

	/// <inheritdoc cref="CalculateLight(RayTracer.Core.HitRecord)"/>
	/// <param name="ray">Output calculated ray for the light ray</param>
	#pragma warning disable CS1573
	public abstract Colour CalculateLight(HitRecord hit, out Ray ray);
	#pragma warning restore CS1573

	/// <summary>Returns an position in world-space, that is used to check if there is an intersection between the hit and the returned point.</summary>
	/// <param name="hit">Information about the hit that will be checked. May be useful for biasing towards the closest point</param>
	/// <remarks>Can be overridden for lights that cover a volume, e.g. area and diffuse lights</remarks>
	[PublicAPI]
	public abstract Vector3 ChooseIntersectTestPosition(HitRecord hit);

	/// <summary>
	///  Returns if there is an intersection between a <paramref name="hit"/> and another <paramref name="position"/>. This overload also allows access to
	///  the computed shadow ray
	/// </summary>
	/// <param name="hit">Hit information</param>
	/// <param name="position">A point on the surface of the light</param>
	/// <param name="shadowRay">Computed shadow ray (<paramref name="hit"/> --> <paramref name="position"/>)</param>
	/// <param name="distance">Distance between the two points</param>
	public bool CheckIntersection(HitRecord hit, Vector3 position, out Ray shadowRay, out float distance)
	{
		//Find ray between the hit and the light
		shadowRay = Ray.FromPoints(hit.WorldPoint, position);
		const float kMin = 0.0001f;
		float       kMax = distance = Vector3.Distance(position, hit.WorldPoint);
		return Renderer.AnyIntersectionFast(shadowRay, kMin, kMax);
	}

	/// <summary>Returns if there is an intersection between a <paramref name="hit"/> and another <paramref name="position"/></summary>
	public bool CheckIntersection(HitRecord hit, Vector3 position) => CheckIntersection(hit, position, out _, out _);

	/// <summary>Returns if there is an intersection between a <paramref name="hit"/> and another <paramref name="position"/></summary>
	public bool CheckIntersection(HitRecord hit, Vector3 position, out Ray shadowRay) => CheckIntersection(hit, position, out shadowRay, out _);
}