using RayTracer.Core.Acceleration;
using System.Numerics;
using static System.MathF;
using static System.Numerics.Vector3;

namespace RayTracer.Core.Hittables;

/// <summary>
///  Implementation of <see cref="Hittable"/> for a sphere
/// </summary>
public sealed record Sphere(Vector3 Centre, float Radius) : Hittable
{
	/// <inheritdoc/>
	public override AxisAlignedBoundingBox BoundingVolume { get; } = new(Centre - new Vector3(Radius), Centre + new Vector3(Radius));

	/// <inheritdoc/>
	public override HitRecord? TryHit(Ray ray, float kMin, float kMax)
	{
		//Do some ray-sphere intersection math to find if the ray intersects
		Vector3 rayPos = ray.Origin, rayDir = ray.Direction;
		Vector3 oc     = rayPos - Centre;
		float   a      = rayDir.LengthSquared();
		float   halfB  = Dot(oc, rayDir);
		float   c      = oc.LengthSquared() - (Radius * Radius);

		float discriminant = (halfB * halfB) - (a * c);
		if (discriminant < 0) return null; //No solutions to where ray intersects with sphere because of negative square root

		float sqrtD = Sqrt(discriminant);

		// Find the nearest root that lies in the acceptable range.
		//This way we do a double check on both, prioritizing the less-positive root (as it's closer)
		//And we only return null if neither is valid
		float root = (-halfB - sqrtD) / a;
		if ((root < kMin) || (kMax < root) || root is float.NaN)
		{
			root = (-halfB + sqrtD) / a;
			if ((root < kMin) || (kMax < root)||root is float.NaN) return null;
		}

		float   k             = root;                            //How far along the ray we had to go to hit the sphere
		Vector3 worldPoint    = ray.PointAt(k);                  //Closest point on the surface of the sphere that we hit (world space)
		Vector3 localPoint    = worldPoint - Centre;             //Same as above but from the centre of this sphere
		Vector3 outwardNormal = Normalize(localPoint);           //Normal direction at the point. Will always face outwards
		bool    inside        = Dot(rayDir, outwardNormal) > 0f; //If the ray is 'inside' the sphere

		//This flips the normal if the ray is inside the sphere
		//This forces the normal to always be going against the ray
		Vector3   normal = inside ? -outwardNormal : outwardNormal;
		Vector2   uv     = GetSphereUV(outwardNormal);
		HitRecord hit    = new(ray, worldPoint, localPoint, normal, k, !inside, uv);
		return hit;
	}

	/// <summary>
	///  Gets UV coordinates from a point <paramref name="p"/> on the sphere's surface. The point must be on the surface of a sphere centred at (0, 0, 0),
	///  with a radius > 0
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

		float theta = Acos(-p.Y);
		float phi   = Atan2(-p.Z, p.X) + PI;

		float u = phi   / (2 * PI);
		float v = theta / PI;
		return new Vector2(u, v);
	}

	/// <inheritdoc/>
	public override string ToString() => $"Sphere {{Radius: {Radius}}}";
}