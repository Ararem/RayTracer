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
/// <param name="SceneObject">The object in the scene that was hit</param>
/// <param name="Material"><see cref="RayTracer.Core.Material"/> that will be used to render this hit</param>
public readonly record struct HitRecord(
		Ray     Ray,
		Vector3 WorldPoint,
		Vector3 LocalPoint, //TODO: Remove LocalPoint? Perhaps also WorldPoint since it can be found from the ray and K value
		Vector3 Normal,
		float   K,
		bool    OutsideFace,
		Vector2 UV,
		SceneObject SceneObject,
		Material Material
);