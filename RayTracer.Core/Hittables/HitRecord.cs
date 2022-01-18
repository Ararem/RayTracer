using RayTracer.Core.Graphics;
using System.Numerics;

namespace RayTracer.Core.Hittables;

public record struct HitRecord(
		Ray Ray,
		Vector3 Point,
		Vector3 Normal,
		float   K,
		bool frontFace
		// ,Vector2 UV
				);