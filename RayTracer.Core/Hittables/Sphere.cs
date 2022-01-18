using RayTracer.Core.Graphics;
using System.Numerics;

namespace RayTracer.Core.Hittables;

/// <summary>
/// Implementation of <see cref="Hittable"/> for a sphere
/// </summary>
public sealed class Sphere : Hittable
{
	public Vector3 Centre;
	public float   Radius;

	/// <inheritdoc/>
	public override HitRecord? TryHit(Ray ray)
	{
		//Do some ray-sphere intersection math to find if the ray intersects
		Vector3 rayPos = ray.Origin, rayDir = ray.Direction;
		Vector3 oc     = rayPos - Centre;
		float   a      = rayDir.LengthSquared();
		float   halfB  = Vector3.Dot(oc, rayDir);
		float   c      = oc.LengthSquared() - (Radius * Radius);

		float discriminant = (halfB * halfB) - (a * c);
		//No solution to the quadratic
		if (discriminant < 0)
			return null;
		float   k      = (-halfB - MathF.Sqrt(discriminant) ) / a;
		Vector3 normal = Vector3.Normalize(ray.PointAt(k) - new Vector3(0, 0, -1));
		return new(k, normal);
	}
}