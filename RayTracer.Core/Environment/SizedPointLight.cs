using RayTracer.Core.Hittables;
using System.Numerics;

namespace RayTracer.Core.Environment;

/// <summary>
///  Represents an infinitely small light source at a certain <see cref="Position"/> in world-space. The light does not take distance into account, so
///  the light will be equally bright at any distance away from it's <see cref="Position"/>
/// </summary>
/// <param name="Position">Where the light source is located in world-space</param>
/// <param name="Colour">Colour of the emitted light</param>
/// <param name="Radius">Colour of the emitted light</param>
/// <param name="DistanceScaleLimit">
///  Limit for how large the brightness increase can get when very close to the light source. Having this at a higher value means the scene is more
///  realistic (as it follows nature better), but it can cause scene noise from excessively bright pixels being reflected.
/// </param>
/// <param name="SurfaceDirectionImportance">
///  Value that affects how important it is for the surface to point towards the light source ([0...1]). 0 means the direction is not taken into account,
///  and 1 means the direction is accounted for as normal.
/// </param>
/// <param name="DistanceImportance">
///  Value that affects how important it is for the surface to be close to the light source ([0...1]). 0 means the distance is not taken into account,
///  and 1 means the distance is accounted for following the inverse-square law.
/// </param>
public record SizedPointLight(Vector3 Position, Colour Colour, float Radius, float DistanceScaleLimit = 10f, float SurfaceDirectionImportance = 1f, float DistanceImportance = 1f) : Light
{
	/// <inheritdoc/>
	public override Colour CalculateLight(HitRecord hit, AsyncRenderJob renderer)
	{
		//See if there's anything in between us and the object
		if (!CheckIntersection(hit, Position, renderer, out Ray shadowRay)) //Returns false if no intersection found, meaning unrestricted path
		{
			Colour colour    = Colour;
			float  dot       = Vector3.Dot(shadowRay.Direction, hit.Normal);
			if (dot < 0) dot = -dot;                                             //Backfaces give negative dot product
			colour *= MathUtils.Lerp(1, dot, SurfaceDirectionImportance);        //Account for how much the surface points towards our light
			float distSqr   = Vector3.DistanceSquared(hit.WorldPoint, Position); //Normally formula uses R^2, so don't bother rooting here to save performance
			float distScale = (Radius * Radius) / distSqr;                       // Inverse square law
			distScale =  MathF.Min(distScale, DistanceScaleLimit);
			colour    *= MathUtils.Lerp(1, distScale, DistanceImportance); //Account for inverse square law
			return colour;
		}
		else
		{
			return Colour.Black;
		}
	}
}