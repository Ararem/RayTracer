using System.Numerics;

namespace RayTracer.Core;

/// <summary>Record containing information about when a <see cref="Core.Ray"/> intersects with a <see cref="Hittable"/></summary>
/// <param name="Ray">The ray that intersected with the <see cref="Hittable"/></param>
/// <param name="LocalPoint">The point on the surface of the object where the intersection occured (relative to the object's centre, so object-space)</param>
/// <param name="WorldPoint">The point on the surface of the object where the intersection occured (relative to the centre of the scene, so world-space)</param>
/// <param name="Normal">The surface normal at the intersection. Should point away from the centre of the object (outwards)</param>
/// <param name="K">Distance along the ray at which the intersection occured</param>
/// <param name="OutsideFace">If the object was hit on the outside of it's surface, as opposed to it's inside</param>
/// <param name="UV">UV coordinate of the hit (for texture mapping)</param>
/// <param name="Material"><see cref="RayTracer.Core.Material"/> that will be used to render this hit</param>
/// <param name="ShaderData">Optional object containing information to be passed into the shader, may be useful communicating between the <see cref="Hittable"/> and the <see cref="Material"/> on a per-hit basis</param>
public readonly record struct HitRecord(
		Ray         Ray,
		Vector3     WorldPoint,
		Vector3     LocalPoint, //TODO: Remove LocalPoint? Perhaps also WorldPoint since it can be found from the ray and K value
		Vector3     Normal,
		float       K,
		bool        OutsideFace,
		Vector2     UV,
		Material    Material,
		object? ShaderData = null
);

/// <summary>
/// Exception class for when the <see cref="HitRecord.ShaderData"/> is invalid
/// </summary>
public class InvalidShaderDataException : Exception
{
	/// <summary>
	/// Actual value that was supplied as the <see cref="HitRecord.ShaderData"/> value
	/// </summary>
	public object? ActualValue { get; }

	/// <inheritdoc />
	public InvalidShaderDataException(object? actualValue, string? message = null, Exception? innerException = null) : base(message, innerException)
	{
		ActualValue  = actualValue;
	}
}