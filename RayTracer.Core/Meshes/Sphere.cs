using RayTracer.Core.Graphics;
using System.Numerics;

namespace RayTracer.Core.Meshes;

public class Sphere : Mesh
{
	public Vector3 Centre;
	public float   Radius;

	/// <inheritdoc/>
	public override bool Hit(Ray ray)
	{
		Vector3 oc           = ray.Origin - Centre;
		float   a            = Vector3.Dot(ray.Direction, ray.Direction);
		double  b            = 2.0 * Vector3.Dot(oc, ray.Direction);
		float   c            = Vector3.Dot(oc,       oc) - (Radius * Radius);
		double  discriminant = (b * b) - (4 * a * c);
		return discriminant > 0;
	}
}