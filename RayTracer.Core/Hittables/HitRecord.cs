using System.Numerics;

namespace RayTracer.Core.Hittables;

/// <summary>
///  Record containing information about when a <see cref="Graphics.Ray"/> intersects with a <see cref="HittableBase"/>
/// </summary>
/// <param name="LocalPoint">The point on the surface of the object where the intersection occured (relative to the object's centre, so object-space)</param>
/// <param name="WorldPoint">The point on the surface of the object where the intersection occured (relative to the centre of the scene, so world-space)</param>
/// <param name="Normal">The surface normal at the intersection</param>
/// <param name="K">Distance along the ray at which the intersection occured</param>
/// <param name="OutsideFace">If the object was hit on the outside of it's surface, as opposed to it's inside</param>
public readonly record struct HitRecord(
		Vector3 WorldPoint,
		Vector3 LocalPoint,
		Vector3 Normal,
		float   K,
		bool    OutsideFace,
		Vector2 UV
);