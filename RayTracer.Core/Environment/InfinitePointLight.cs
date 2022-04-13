using RayTracer.Core.Hittables;
using System.Numerics;

namespace RayTracer.Core.Environment;

/// <summary>
/// Represents an infinitely small light source at a certain <see cref="Position"/> in world-space. The light does not take distance into account, so the light will be equally bright at any distance away from it's <see cref="Position"/>
/// </summary>
/// <param name="Position">Where the light source is located in world-space</param>
/// <param name="Colour">Colour of the emitted light</param>
public sealed record InfinitePointLight(Vector3 Position, Colour Colour) : Light
{
	/// <inheritdoc />
	public override Colour CalculateLight(HitRecord hit, Func<Ray, (SceneObject sceneObject, HitRecord hit)?> findClosestIntersection)
	{
		//See if there's anything in between us and the object
		Ray                                       shadowRay    = Ray.FromPoints(hit.WorldPoint, Position);
		(SceneObject sceneObject, HitRecord hit)? intersection = findClosestIntersection(shadowRay);
		if (intersection is null || intersection.Value.hit.K <= Position.Length()) //Null if no intersection found, meaning unrestricted path
		{
			Colour colour = Colour;
			float  dot    = MathF.Abs(Vector3.Dot(shadowRay.Direction, hit.Normal));
			colour *= dot; //Account for how much the surface points towards our light
			return colour;
		}
		else
		{
			return Colour.Black;
		}
	}
}