using RayTracer.Core;
using System.Numerics;

namespace RayTracer.Impl.Lights;

//TODO: Combine with DiffuseSphere light and add diffusion parameter
/// <summary>
///  Represents a light source at a certain <see cref="Position"/> in world-space. The light has an artificial size defined by the <see cref="Radius"/> -
///  points are randomly chosen inside on sphere centred at <see cref="Position"/> with a radius of <see cref="Radius"/>.
/// </summary>
public sealed class SurfaceSphereLight : Light
{
	/// <summary>
	///  Represents a light source at a certain <see cref="Position"/> in world-space. The light has an artificial size defined by the <see cref="Radius"/> -
	///  points are randomly chosen inside on sphere centred at <see cref="Position"/> with a radius of <see cref="Radius"/>.
	/// </summary>
	/// <param name="position">Where the light source is located in world-space</param>
	/// <param name="colour">Colour of the emitted light</param>
	/// <param name="brightnessBaselineRadius">The radius at which the brightness is considered baseline (1)</param>
	/// <param name="radius">How large of an area the light occupies (radius at which points will be chosen for shadow testing)</param>
	/// <param name="distanceScaleLimit">
	///  Limit for how large the brightness increase can get when very close to the light source. Having this at a higher value means the scene is more
	///  realistic (as it follows nature better), but it can cause scene noise from excessively bright pixels being reflected.
	/// </param>
	/// <param name="surfaceDirectionImportance">
	///  Value that affects how important it is for the surface to point towards the light source ([0...1]). 0 means the direction is not taken into account,
	///  and 1 means the direction is accounted for as normal.
	/// </param>
	/// <param name="distanceImportance">
	///  Value that affects how important it is for the surface to be close to the light source ([0...1]). 0 means the distance is not taken into account,
	///  and 1 means the distance is accounted for following the inverse-square law.
	/// </param>
	public SurfaceSphereLight(Vector3 position, float radius, Colour colour, float brightnessBaselineRadius, float distanceScaleLimit = 10f, float surfaceDirectionImportance = 1f, float distanceImportance = 1f)
	{
		Position                   = position;
		Radius                     = radius;
		Colour                     = colour;
		BrightnessBaselineRadius   = brightnessBaselineRadius;
		DistanceScaleLimit         = distanceScaleLimit;
		SurfaceDirectionImportance = surfaceDirectionImportance;
		DistanceImportance         = distanceImportance;
	}

	/// <summary>Where the light source is located in world-space</summary>
	public Vector3 Position { get; }

	/// <summary>How large of an area the light occupies (radius at which points will be chosen for shadow testing)</summary>
	public float Radius { get; }

	/// <summary>Colour of the emitted light</summary>
	public Colour Colour { get; }

	/// <summary>The radius at which the brightness is considered baseline (1)</summary>
	public float BrightnessBaselineRadius { get; }

	/// <summary>
	///  Limit for how large the brightness increase can get when very close to the light source. Having this at a higher value means the scene is more
	///  realistic (as it follows nature better), but it can cause scene noise from excessively bright pixels being reflected.
	/// </summary>
	public float DistanceScaleLimit { get; }

	/// <summary>
	///  Value that affects how important it is for the surface to point towards the light source ([0...1]). 0 means the direction is not taken into account,
	///  and 1 means the direction is accounted for as normal.
	/// </summary>
	public float SurfaceDirectionImportance { get; }

	/// <summary>
	///  Value that affects how important it is for the surface to be close to the light source ([0...1]). 0 means the distance is not taken into account,
	///  and 1 means the distance is accounted for following the inverse-square law.
	/// </summary>
	public float DistanceImportance { get; }

	/// <inheritdoc/>
	public override Colour CalculateLight(HitRecord hit)
	{
		//See if there's anything in between us and the object
		Ray         shadowRay    = Ray.FromPoints(hit.WorldPoint, Position); //Hit towards centre of light
		const float kMin         = 0.0001f;
		float       kMax         = Vector3.Distance(hit.WorldPoint, Position) - Radius; //Subtract radius to get the point on the side closest to the hit
		bool        intersection = Renderer.AnyIntersectionFast(shadowRay, kMin, kMax);
		if (!intersection) //Returns false if no intersection found, meaning unrestricted path
		{
			Colour colour    = Colour;
			float  dot       = Vector3.Dot(shadowRay.Direction, hit.Normal);
			if (dot < 0) dot = -dot;                                                            //Backfaces give negative dot product
			colour *= MathUtils.Lerp(1, dot, SurfaceDirectionImportance);                       //Account for how much the surface points towards our light
			float distSqr   = Vector3.DistanceSquared(hit.WorldPoint, shadowRay.PointAt(kMax)); //Normally formula uses R^2, so don't bother rooting here to save performance
			float distScale = (BrightnessBaselineRadius * BrightnessBaselineRadius) / distSqr;  // Inverse square law
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