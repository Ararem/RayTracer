using RayTracer.Core.Hittables;
using System.Numerics;

namespace RayTracer.Core.Environment;

/// <summary>
///  Base class that defines a light that can be used to calculate lighting more accurately in the scene
/// </summary>
public abstract record Light
{
	/// <summary>
	///  Fast method that simply checks if there is any intersection along a given <paramref name="ray"/>, in the range specified by [<paramref name="kMin"/>
	///  ..<paramref name="kMax"/>]
	/// </summary>
	/// <remarks>
	///  Use this for simple shadow-like checks, to see if <i>anything</i> lies in between the light source and target point. Note that this will return
	///  <see langword="true"/> as soon as an intersection is hit, and does not take into account a material's properties (such as transparency), just
	///  geometry.
	/// </remarks>
	public delegate bool FastAnyIntersectCheck(Ray ray, float kMin, float kMax);

	/// <summary>
	///  The big brother of <see cref="FastAnyIntersectCheck"/>, does a full raytrace along the given <paramref name="ray"/>, finding the closest
	///  intersection along the ray int the range, as well as what object was hit.
	/// </summary>
	/// <remarks>
	///  As the name shows, this method is
	///  <i>
	///   <b>SLOW</b>
	///  </i>
	///  . Only use it when you really have to, because this is going to massively increase the render time due to how slow the method is
	/// </remarks>
	public delegate (SceneObject Object, HitRecord HitRecord)? SlowClosestIntersectCheck(Ray ray, float kMin, float kMax);

	/// <summary>
	///  Calculates the light emitted by the current <see cref="Light"/> instance, for the hit stored in the <paramref name="hit"/>
	/// </summary>
	/// <param name="hit">The information about the hit to calculate the lighting for</param>
	/// <param name="fastAnyIntersectCheck">Simple method to quickly check if there's any intersection along a given ray in a range</param>
	/// <param name="slowClosestIntersectCheck">
	///  A function that can be used to calculate the closest intersection along a given ray. Returns the closest intersection and the object intersected
	///  with when possible, or <see langword="unll"/> when no intersection along the ray was found (with regards to <see cref="RenderOptions"/>.
	///  <see cref="RenderOptions.KMin"/> and <see cref="RenderOptions"/>.<see cref="RenderOptions.KMax"/>
	/// </param>
	/// <returns>The amount of light received by the <paramref name="hit"/>, from this light source</returns>
	/// <remarks>
	///  When checking for shadowing, you would most likely use <paramref name="fastAnyIntersectCheck"/> between the point hit and the position of the light
	///  source (see example below)
	/// </remarks>
	/// <example>
	///  Assuming a light has a <c>Vector3 Position</c> property.
	///  <code>
	/// public override Colour CalculateLight(HitRecord hit, FastAnyIntersectCheck fastAnyIntersectCheck, SlowClosestIntersectCheck slowClosestIntersectCheck)
	/// {
	/// 	Vector3 Position = this.Position;
	/// 	//Find ray between the hit and the light
	/// 	Ray         shadowRay = Ray.FromPoints(hit.WorldPoint, Position);
	/// 	const float kMin      = 0.0001f;
	/// 	float       kMax      = Vector3.Distance(Position, hit.WorldPoint);
	/// 	if (fastAnyIntersectCheck(shadowRay, kMin, kMax)) //Shadowed
	/// 		return Colour.Black;
	/// 	else //No shadow
	/// 		return LightColour;
	/// }
	///  </code>
	/// </example>
	/// <seealso cref="SlowClosestIntersectCheck"/>
	/// <seealso cref="FastAnyIntersectCheck"/>
	public abstract Colour CalculateLight(HitRecord hit, FastAnyIntersectCheck fastAnyIntersectCheck, SlowClosestIntersectCheck slowClosestIntersectCheck);

	/// <summary>
	/// Returns if there is an intersection between a <see cref="hit"/> and another <see cref="position"/>. This overload also allows access to the computed shadow ray
	/// </summary>
	public static bool CheckIntersection(HitRecord hit, Vector3 position, FastAnyIntersectCheck fastAnyIntersectCheck, out Ray shadowRay)
	{
		//Find ray between the hit and the light
		shadowRay = Ray.FromPoints(hit.WorldPoint, position);
		const float kMin      = 0.0001f;
		float       kMax      = Vector3.Distance(position, hit.WorldPoint);
		return fastAnyIntersectCheck(shadowRay, kMin, kMax);
	}

	/// <summary>
	/// Returns if there is an intersection between a <see cref="hit"/> and another <see cref="position"/>
	/// </summary>
	public static bool CheckIntersection(HitRecord hit, Vector3 position, FastAnyIntersectCheck fastAnyIntersectCheck)
	{
		//Find ray between the hit and the light
		Ray         shadowRay = Ray.FromPoints(hit.WorldPoint, position);
		const float kMin      = 0.0001f;
		float       kMax      = Vector3.Distance(position, hit.WorldPoint);
		return fastAnyIntersectCheck(shadowRay, kMin, kMax);
	}
}