using RayTracer.Core.Graphics;
using System.Numerics;

namespace RayTracer.Core.Hittables;

/// <summary>
///  Implementation of <see cref="Hittable"/> for a sphere
/// </summary>
public sealed class Sphere : Hittable
{
	/// <summary>
	///  The radius of the sphere (distance from centre to it's surface)
	/// </summary>
	public float Radius;

	/// <inheritdoc/>
	public override HitRecord? TryHit(Ray ray, float kMin, float kMax)
	{
		//Do some ray-sphere intersection math to find if the ray intersects
		//Since (due to design decisions) the objects are treated as if they are always centred at (0,0,0) from the hittable's point of view
		//We don't have to account for any offsets here
		Vector3 rayPos = ray.Origin, rayDir = ray.Direction;
		float   a      = rayDir.LengthSquared();
		float   halfB  = Vector3.Dot(rayPos, rayDir);
		float   c      = rayPos.LengthSquared() - (Radius * Radius);

		float discriminant = (halfB * halfB) - (a * c);
		if (discriminant < 0) return null; //No solutions to where ray intersects with sphere because of negative square root

		float sqrtD = MathF.Sqrt(discriminant);

		// Find the nearest root that lies in the acceptable range.
		//This way we do a double check on both, prioritizing the less-positive root (as it's closer)
		//And we only return null if neither is valid
		float root = (-halfB - sqrtD) / a;
		if ((root < kMin) || (kMax < root))
		{
			root = (-halfB + sqrtD) / a;
			if ((root < kMin) || (kMax < root)) return null;
		}

		float   k             = root;                                    //How far along the ray we had to go to hit the sphere
		Vector3 point         = ray.PointAt(k);                          //Closest point on the surface of the sphere that we hit
		Vector3 outwardNormal = point / Radius;                          //Normal direction at the point. Will always face outwards
		bool    inside        = Vector3.Dot(rayDir, outwardNormal) > 0f; //If the ray is 'inside' the sphere

		//This flips the normal if the ray is inside the sphere
		//This forces the normal to always be going against the ray
		Vector3 normal = inside ? -outwardNormal : outwardNormal;
		Vector2 uv     = GetSphereUV(outwardNormal);
		return new HitRecord(point, normal, k, !inside, uv);
	}

	/// <summary>
	///  Gets UV coordinates from a point <paramref name="p"/> on the sphere's surface
	/// </summary>
	/// <param name="p">The point on the sphere's surface</param>
	public static Vector2 GetSphereUV(Vector3 p)
	{
		// p: a given point on the sphere of radius one, centered at the origin.
		// u: returned value [0,1] of angle around the Y axis from X=-1.
		// v: returned value [0,1] of angle from Y=-1 to Y=+1.
		//     <1 0 0> yields <0.50 0.50>       <-1  0  0> yields <0.00 0.50>
		//     <0 1 0> yields <0.50 1.00>       < 0 -1  0> yields <0.50 0.00>
		//     <0 0 1> yields <0.25 0.50>       < 0  0 -1> yields <0.75 0.50>

		float theta = MathF.Acos(-p.Y);
		float phi   = MathF.Atan2(-p.Z, p.X) + MathF.PI;

		float u = phi / (2 * MathF.PI);
		float v = theta / MathF.PI;
		return new Vector2(u, v);
	}

	/// <inheritdoc />
	public override string ToString() => $"Sphere {{Radius: {Radius}}}";
}