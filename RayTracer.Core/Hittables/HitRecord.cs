using System.Numerics;

namespace RayTracer.Core.Hittables;

public record struct HitRecord(
		float K,
		Vector3 Normal);