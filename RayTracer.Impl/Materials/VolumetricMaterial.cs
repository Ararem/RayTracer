using RayTracer.Core;
using RayTracer.Impl.Hittables;

namespace RayTracer.Impl.Materials;

/// <summary>
///  A material that scatters in all directions. Should only be used when paired with a volumetric mesh, such as a
///  <see cref="ConstantDensityMedium"/>
/// </summary>
/// <remarks>Scatter direction is completely random</remarks>
public sealed class VolumetricMaterial : Material
{
	/// <summary>
	///  A material that scatters in all directions. Should only be used when paired with a volumetric mesh, such as a
	///  <see cref="ConstantDensityMedium"/>
	/// </summary>
	/// <remarks>Scatter direction is completely random</remarks>
	public VolumetricMaterial(Colour albedo)
	{
		Albedo = albedo;
	}

	/// <summary>Colour of the material</summary>
	public Colour Albedo { get; }

	/// <inheritdoc/>
	public override Ray? Scatter(HitRecord hit, ArraySegment<HitRecord> previousHits) => new Ray(hit.WorldPoint, RandUtils.RandomOnUnitSphere());

	/// <inheritdoc/>
	public override Colour CalculateColour(Colour colour, HitRecord hit, ArraySegment<HitRecord> previousHits) => colour * Albedo;
}