using JetBrains.Annotations;

namespace RayTracer.Core;

/// <summary>Base class that defines a light that can be used to calculate lighting more accurately in the scene</summary>
[PublicAPI]
public abstract class Light : RenderAccessor
{
	/// <summary>Calculates the light emitted by the current <see cref="Light"/> instance, for the hit stored in the <paramref name="hit"/></summary>
	/// <param name="hit">The information about the hit to calculate the lighting for</param>
	/// <param name="ray">Output shadow ray that was calculated and checked</param>
	/// <param name="calculateRawColour">
	///  Optional flag to make the method return the "raw" colour of the light for the hit. E.g. the light should not apply any distance attenuation if this
	///  is set to <see langword="true"/>
	/// </param>
	/// <returns>The amount of light received by the <paramref name="hit"/>, from this light source</returns>
	/// <remarks>
	///  When checking for shadowing, you would most likely use <see cref="AsyncRenderJob.AnyIntersectionFast"/> between the point hit and the position of
	///  the light source (see example below)
	/// </remarks>
	/// <example>
	///  Assuming a light has a <c>Vector3 Position</c> property.
	///  <code>
	/// //(Some code omitted)
	/// Vector3 Position = this.Position;
	/// //Find ray between the hit and the light
	/// Ray         shadowRay = Ray.FromPoints(hit.WorldPoint, Position);
	/// const float kMin      = 0.0001f;
	/// float       kMax      = Vector3.Distance(Position, hit.WorldPoint);
	/// if (renderer.AnyIntersectionFast(shadowRay, kMin, kMax)) //Shadowed
	/// 	return Colour.Black;
	/// else //No shadow
	/// 	return LightColour;
	///  </code>
	/// </example>
	public abstract Colour CalculateLight(HitRecord hit, out Ray ray, bool calculateRawColour = false);
}