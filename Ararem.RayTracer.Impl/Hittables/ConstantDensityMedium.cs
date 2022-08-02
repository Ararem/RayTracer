#region

using Ararem.RayTracer.Core;
using Ararem.RayTracer.Core.Acceleration;
using System.Numerics;

#endregion

namespace Ararem.RayTracer.Impl.Hittables;

/// <summary>
///  A hittable that has a constant density (aka a volume like a cloud). Should be used in conjunction with a <see cref="VolumetricMaterial"/>
/// </summary>
/// <remarks>This probably won't work too well with rays inside the medium, or other objects inside it, so beware...</remarks>
public sealed class ConstantDensityMedium : Hittable
{
	private readonly VolumetricMaterial InternalMaterial;

	/// <summary>-1 / <see cref="Density"/></summary>
	private readonly float negInvDensity;

	/// <summary>Default constructor</summary>
	public ConstantDensityMedium(Hittable boundary, float density, Colour colour)
	{
		if (boundary is ConstantDensityMedium) throw new ArgumentException("Cannot create a constant density volume using another volume", nameof(boundary));
		Boundary         = boundary;
		Density          = density;
		negInvDensity    = -1f / density;
		InternalMaterial = new VolumetricMaterial(colour, density);
	}

	public Material Material => InternalMaterial;

	/// <inheritdoc/>
	public override RenderJob Renderer
	{
		get => base.Renderer;
		set
		{
			Material.Renderer = value;
			base.Renderer     = value;
		}
	}

	/// <inheritdoc/>
	public override HitRecord? TryHit(Ray ray, float kMin, float kMax)
	{
		//Where we enter the volume
		if (Boundary.TryHit(ray, kMin, kMax) is not {} entryHit) return null;
		//Where we exit the volume
		if (Boundary.TryHit(ray, entryHit.K + 0.001f, kMax) is not {} exitHit) return null;

		//Find how far we travelled between the inside and outside
		float distanceBetweenHits = exitHit.K - entryHit.K;
		//Random distance for how far we're going to travel this time (how far along the ray will we intersect with the medium)
		float distanceToIntersection = negInvDensity * MathF.Log(RandUtils.RandomFloat01());

		if (distanceToIntersection > distanceBetweenHits) return null; //If we intersect outside the boundary, nothing happens

		// ReSharper disable InlineTemporaryVariable
		Vector3    worldPoint = ray.PointAt(distanceToIntersection);
		Vector3    localPoint = worldPoint;                     //Arbitrary
		Vector3    normal     = RandUtils.RandomOnUnitSphere(); //Arbitrary
		const bool frontFace  = true;                           //Arbitrary

		//Here we pass in how far we travelled 'inside' the object onto the VolumetricMaterial (the shader)
		return new HitRecord(ray, worldPoint, localPoint, normal, entryHit.K + distanceToIntersection, frontFace, Vector2.Zero, this, Material, distanceToIntersection);
	}

	/// <summary>How dense the medium is. Higher values increase the chance of collision</summary>
	public float Density { get; }

	/// <summary>The 'shape' of this medium. Can be any shape other than a <see cref="ConstantDensityMedium"/></summary>
	public Hittable Boundary { get; }

	/// <inheritdoc/>
	public override bool FastTryHit(Ray ray, float kMin, float kMax) => Boundary.FastTryHit(ray, kMin, kMax);

	/// <inheritdoc/>
	public override AxisAlignedBoundingBox BoundingVolume => Boundary.BoundingVolume;

	/// <summary>
	///  A material that scatters in all directions. Should only be used when paired with a volumetric mesh, such as a
	///  <see cref="ConstantDensityMedium"/>
	/// </summary>
	/// <remarks>Scatter direction is completely random</remarks>
	private sealed class VolumetricMaterial : Material
	{
		/// <summary>Colour of the material</summary>
		public readonly Colour Albedo;

		/// <summary>How dense the material is</summary>
		public readonly float Density;

		internal VolumetricMaterial(Colour albedo, float density)
		{
			Albedo  = albedo;
			Density = density;
		}

		/// <inheritdoc/>
		public override Ray? Scatter(HitRecord currentHit, ArraySegment<HitRecord> prevHitsBetweenCamera) => new Ray(currentHit.WorldPoint, RandUtils.RandomOnUnitSphere());

		/// <inheritdoc/>
		public override Colour CalculateColour(Colour futureRayColour, Ray futureRay, HitRecord currentHit, ArraySegment<HitRecord> prevHitsBetweenCamera)
		{
			/*
			 * D = 1, A = .5
			 * x=1: out = .5
			 * x=2: out = .5*.5 = .25
			 * x=3: out = .125
			 *
			 * out = (A) ^ x for D = 1
			 *
			 * D = 2, A = .5
			 * x=.5: out = .5
			 * x=1: out = .25
			 *
			 * out = (A) ^ (D * x) for any D
			 */
			//Calculate how much the colour is attenuated
			float distanceInside = currentHit.ShaderData as float? ?? throw new InvalidShaderDataException(currentHit.ShaderData, $"currentHit.ShaderData was not a float (was {currentHit.ShaderData?.GetType()}: {currentHit.ShaderData})");
			float pow            = Density * distanceInside;
			(float albedoR, float albedoG, float albedoB) = Albedo;
			albedoR                                       = MathF.Pow(albedoR, pow);
			albedoG                                       = MathF.Pow(albedoG, pow);
			albedoB                                       = MathF.Pow(albedoB, pow);
			return new Colour(albedoR, albedoG, albedoB) * futureRayColour;
		}
	}
}