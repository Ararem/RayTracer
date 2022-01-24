using RayTracer.Core.Graphics;
using RayTracer.Core.Hittables;

namespace RayTracer.Core.Debugging;

/// <summary>
/// Enum that describes various types of errors that could occur during  graphics rendering
/// </summary>
public enum GraphicsErrorType
{
	/// <summary>
	/// Surface normals for an object had an incorrect magnitude (Magnitude should always be 1)
	/// </summary>
	/// <remarks>
	///Indicates an error with the <see cref="Hittable.TryHit"/> method's normal calculation code
	/// </remarks>
	/// <seealso cref="RayDirectionWrongMagnitude"/>
	NormalsWrongMagnitude,
	/// <summary>
	/// The<see cref="Ray.Direction"/> of a <see cref="Ray"/> has a magnitude not equal to 1 
	/// </summary>
	/// <seealso cref="NormalsWrongMagnitude"/>
	RayDirectionWrongMagnitude,
	/// <summary>
	/// The <see cref="HitRecord.K"/> value for a <see cref="HitRecord"/> was outside the valid range [<see cref="RenderOptions.KMin"/>..<see cref="RenderOptions.KMax"/>]
	/// </summary>
	/// <remarks>
	///Indicates that the ray-object intersection code for a <see cref="Hittable"/> is incorrect (most likely it returns the closes point and does not validate their distance along the ray)
	/// </remarks>
	KValueNotInRange
}