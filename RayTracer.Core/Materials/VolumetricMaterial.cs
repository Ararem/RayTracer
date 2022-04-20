using RayTracer.Core.Hittables;

namespace RayTracer.Core.Materials;

/// <summary>
///  A material that scatters in all directions. Should only be used when paired with a volumetric mesh, such as a <see cref="ConstantDensityMedium"/>
/// </summary>
/// <remarks>Scatter direction is completely random</remarks>
public record VolumetricMaterial(Colour Albedo) : Material
{
	/// <inheritdoc/>
	public override Ray? Scatter(HitRecord hit) => new Ray(hit.WorldPoint, RandUtils.RandomOnUnitSphere());

	/// <inheritdoc/>
	public override void DoColourThings(ref Colour colour, HitRecord hit, ArraySegment<(SceneObject sceneObject, HitRecord hitRecord)> previousHits)
	{
		colour *= Albedo;
	}
}