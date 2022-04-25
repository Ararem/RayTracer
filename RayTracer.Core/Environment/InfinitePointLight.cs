using RayTracer.Core.Hittables;
using System.Numerics;

namespace RayTracer.Core.Environment;

/// <summary>
///  Represents an infinitely small light source at a certain <see cref="Position"/> in world-space. The light does not take distance into account, so
///  the light will be equally bright at any distance away from it's <see cref="Position"/>
/// </summary>
/// <param name="Position">Where the light source is located in world-space</param>
/// <param name="Colour">Colour of the emitted light</param>
/// <param name="SurfaceDirectionImportance">
///  Value that affects how important it is for the surface to point towards the light source ([0...1]). 0 means the direction is not taken into account,
///  and 1 means the direction is accounted for as normal.
/// </param>
public sealed record InfinitePointLight(Vector3 Position, Colour Colour, float SurfaceDirectionImportance = 1f) : Light
{
	/// <inheritdoc/>
	public override Colour CalculateLight(HitRecord hit, AsyncRenderJob renderer)
	{
		//See if there's anything in between us and the object
		if (!CheckIntersection(hit, Position, out Ray shadowRay)) //Returns false if no intersection found, meaning unrestricted path
		{
			Colour colour    = Colour;
			float  dot       = Vector3.Dot(shadowRay.Direction, hit.Normal);
			if (dot < 0) dot = -dot;                                      //Backfaces give negative dot product
			colour *= MathUtils.Lerp(1, dot, SurfaceDirectionImportance); //Account for how much the surface points towards our light
			return colour;
		}
		else
		{
			return Colour.Black;
		}
	}
}