using RayTracer.Core;

namespace RayTracer.Impl.Materials;

/// <summary>
///  A material (such as glass) that refracts light rays going through it. This is an extended version of the <see cref="RefractiveMaterial"/>, as it
///  supports emissive textures as well
/// </summary>
/// <remarks>
///  Note on in/direct emission: Direct emission is when the light ray has only travelled through this object, e.g. the ray is directly from the camera,
///  pointing at this object. Indirect emission is when the ray has already bounced off another object, such as a wall. Indirect emission is much more
///  nice to look at, since it doesn't blow up the object's colour completely, but still gives a nice glow to it's surroundings.
/// </remarks>
public sealed class EmissiveRefractiveMaterial : RefractiveMaterial
{
	/// <summary>
	///  A material (such as glass) that refracts light rays going through it. This is an extended version of the <see cref="RefractiveMaterial"/>, as it
	///  supports emissive textures as well
	/// </summary>
	/// <param name="refractiveIndex">Refractive index of the material to simulate</param>
	/// <param name="tint">Texture to tint the rays by</param>
	/// <param name="emission">Texture for the colour of the emitted light</param>
	/// <param name="directEmission">Option for enabling direct emission (see remarks)</param>
	/// <param name="alternateRefractionMode">Optional flag that enables an alternate mode of calculating refractions (careful, it's funky)</param>
	/// <remarks>
	///  Note on in/direct emission: Direct emission is when the light ray has only travelled through this object, e.g. the ray is directly from the camera,
	///  pointing at this object. Indirect emission is when the ray has already bounced off another object, such as a wall. Indirect emission is much more
	///  nice to look at, since it doesn't blow up the object's colour completely, but still gives a nice glow to it's surroundings.
	/// </remarks>
	public EmissiveRefractiveMaterial(float refractiveIndex, Texture tint, Texture emission, bool directEmission = false, bool alternateRefractionMode = false) : base(refractiveIndex, tint, alternateRefractionMode)
	{
		Emission       = emission;
		DirectEmission = directEmission;
	}

	/// <summary>Texture for the colour of the emitted light</summary>
	public Texture Emission { get; }

	/// <summary>Option for enabling direct emission (see remarks)</summary>
	public bool DirectEmission { get; }

	/// <inheritdoc/>
	public override Colour CalculateColour(Colour futureRayColour, Ray futureRay, HitRecord currentHit, ArraySegment<HitRecord> prevHitsBetweenCamera)
	{
		Colour colour = futureRayColour;
		colour += CalculateSimpleColourFromLights(currentHit);
		colour *= Tint.GetColour(currentHit);
		//Force emit if we allow direct lighting
		if (DirectEmission)
			return colour + Emission.GetColour(currentHit);
		//Care whether it's direct or not
		else
			switch (prevHitsBetweenCamera.Count)
			{
				//Direct ray from camera
				case 0:
				//Semi indirect - refracted through this object once before
				case 1 when prevHitsBetweenCamera[0].Material == this:
					return colour;
				//Indirect case
				default:
					return colour + Emission.GetColour(currentHit);
			}
	}
}