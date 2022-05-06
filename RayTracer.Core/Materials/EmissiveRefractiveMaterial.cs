using JetBrains.Annotations;
using RayTracer.Core.Hittables;
using RayTracer.Core.Textures;
using System.Numerics;
using static RayTracer.Core.RandUtils;
using static System.Numerics.Vector3;
using static System.MathF;

namespace RayTracer.Core.Materials;

/// <summary>
///  A material (such as glass) that refracts light rays going through it. This is an extended version of the <see cref="RefractiveMaterial"/>, as it
///  supports emissive textures as well
/// </summary>
/// <param name="RefractiveIndex">Refractive index of the material to simulate</param>
/// <param name="Tint">Texture to tint the rays by</param>
/// <param name="Emission">Texture for the colour of the emitted light</param>
/// <param name="DirectEmission">Option for enabling direct emission (see remarks)</param>
/// <remarks>
///  Note on in/direct emission: Direct emission is when the light ray has only travelled through this object, e.g. the ray is directly from the camera,
///  pointing at this object.
///  Indirect emission is when the ray has already bounced off another object, such as a wall. Indirect emission is much more nice to look at, since it
///  doesn't blow up the object's colour completely, but still gives a nice glow to it's surroundings.
/// </remarks>
public sealed record EmissiveRefractiveMaterial(float RefractiveIndex, Texture Tint, Texture Emission, bool DirectEmission = false) : RefractiveMaterial(RefractiveIndex, Tint)
{
	/// <inheritdoc/>
	public override void DoColourThings(ref Colour colour, HitRecord hit, ArraySegment<(SceneObject sceneObject, HitRecord hitRecord)> previousHits)
	{
		colour *= Tint.GetColour(hit);
		//Force emit if we allow direct lighting
		if (DirectEmission)
			colour += Emission.GetColour(hit);
		//Care whether it's direct or not
		else
			switch (previousHits.Count)
			{
				//Direct ray from camera
				case 0:
				//Semi indirect - refracted through this object once before
				case 1 when previousHits[0].sceneObject.Material == this:
					return;
				//Indirect case
				default:
					colour += Emission.GetColour(hit);
					return;
			}
	}
}