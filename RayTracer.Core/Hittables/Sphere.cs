using RayTracer.Core.Graphics;
using System.Numerics;

namespace RayTracer.Core.Hittables;

public class Sphere : Hittable
{
	public Vector3 Centre;
	public float   Radius;

	/// <inheritdoc/>
	public override float Hit(Ray ray)
	{
		//Do some ray-sphere intersection math to find if the ray intersects
		Vector3 rayPos = ray.Origin, rayDir = ray.Direction;
		Vector3 oc     = rayPos - Centre;
		float   a      = rayDir.LengthSquared();
		float   halfB  = Vector3.Dot(oc, rayDir);
		float   c      = oc.LengthSquared() - (Radius * Radius);

		float discriminant = (halfB * halfB) - (a * c);
		if (discriminant < 0)
			return -1f;
		else
			return (-halfB - MathF.Sqrt(discriminant) ) / a;
	}
}