using RayTracer.Core.Graphics;
using System.Numerics;

namespace RayTracer.Core.Meshes;

public class Sphere : Hittable
{
	public Vector3 Centre;
	public float   Radius;

	/// <inheritdoc/>
	public override bool Hit(Ray ray)
	{
		//Do some ray-sphere intersection math to find if the ray intersects
		Vector3 rayPos = ray.Origin, rayDir = ray.Direction;
		Vector3 oc     = rayPos - Centre;
		float   a      = rayDir.LengthSquared();
		float   halfB  = Vector3.Dot(oc, rayDir);
		float   c      = oc.LengthSquared() - (Radius * Radius);

		float discriminant = (halfB * halfB) - (a * c);
		return discriminant > 0;
	}
}