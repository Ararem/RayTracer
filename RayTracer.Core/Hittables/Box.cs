using System.Numerics;
using static System.MathF;

namespace RayTracer.Core.Hittables;

public record Box(Vector3 Min, Vector3 Max, Matrix4x4 WorldToBoxTransform) : Hittable
{
	private readonly Lazy<Matrix4x4> boxToWorldTransform = new(
			() =>
			{
				Matrix4x4.Invert(WorldToBoxTransform, out Matrix4x4 result);
				return result;
			}
	);

	private readonly Lazy<Vector3> centre     = new(() => (Min + Max)          / 2f);
	private readonly Lazy<float>   halfLength = new(() => (Max - Min).Length() / 2f);

	/// <inheritdoc/>
	public override HitRecord? TryHit(Ray ray, float kMin, float kMax)
	{
		// Calcs intersection and exit distances, normal, face and UVs
		// row is the ray origin in world space
		// rdw is the ray direction in world space
		// txx is the world-to-box transformation
		// txi is the box-to-world transformation
		// ro and rd are in world space
		// rad is the half-length of the box
		//
		// oT contains the entry and exit points
		// oN is the normal in world space
		// oU contains the UVs at the intersection point
		// oF contains the index if the intersected face [0..5]

		// convert from world to box space

		Vector4 temp4 = Vector4.Transform(new Vector4(ray.Direction, 0f), WorldToBoxTransform);
		Vector3 rd    = new(temp4.X, temp4.Y, temp4.Z);
		temp4 = Vector4.Transform(new Vector4(ray.Origin, 1f), WorldToBoxTransform);
		Vector3 ro = new(temp4.X, temp4.Y, temp4.Z);

		// ray-box intersection in box space
		Vector3 m = new Vector3(1f) / rd; //NAN?
		Vector3 s = new(
				rd.X < 0f ? 1f : -1f,
				rd.Y < 0f ? 1f : -1f,
				rd.Z < 0f ? 1f : -1f
		);
		Vector3 t1 = m * (-ro + (s * halfLength.Value));
		Vector3 t2 = m * (-ro - (s * halfLength.Value));

		float kNear = Max(Max(t1.X, t1.Y), t1.Z);
		float kFar  = Min(Min(t2.X, t2.Y), t2.Z);

		//Validate our K value ranges
		if ((kNear > kFar) || (kFar < 0f)) return null;
		float k = kNear;
		if ((k < kMin) || (kMax < k))
		{
			k = kFar;
			if ((k < kMin) || (kMax < k)) return null;
		}

		if (float.IsNaN(k)) return null;

		// compute normal (in world space), face and UV
		Vector3 normal;
		Vector2 uv;
		int     faceIndex;
		if ((t1.X > t1.Y) && (t1.X > t1.Z))
		{
			Vector3 v = new(
					boxToWorldTransform.Value.M11,
					boxToWorldTransform.Value.M12,
					boxToWorldTransform.Value.M13
			);
			normal    = v * s.X;
			uv        = new Vector2(ro.Y, ro.Z) + (new Vector2(rd.Y, rd.Z) * t1.X);
			faceIndex = (1 + (int)s.X) / 2;
		}
		else if (t1.Y > t1.Z)
		{
			Vector3 v = new(
					boxToWorldTransform.Value.M21,
					boxToWorldTransform.Value.M22,
					boxToWorldTransform.Value.M23
			);
			normal    = v * s.Y;
			uv        = new Vector2(ro.Z, ro.Z) + (new Vector2(rd.Z, rd.X) * t1.Y);
			faceIndex = (5 + (int)s.Y) / 2;
		}
		else
		{
			Vector3 v = new(
					boxToWorldTransform.Value.M31,
					boxToWorldTransform.Value.M32,
					boxToWorldTransform.Value.M33
			);
			normal    = v * s.Z;
			uv        = new Vector2(ro.X, ro.Y) + (new Vector2(rd.X, rd.Y) * t1.Z);
			faceIndex = (9 + (int)s.Z) / 2;
		}

		Vector3 worldPoint = ray.PointAt(k);
		Vector3 localPoint = worldPoint - centre.Value;

		//
		_ = uv;
		_ = faceIndex;

		//TODO: UV's, outside face
		return new HitRecord(ray, worldPoint, localPoint, normal, k, Vector3.Dot(ray.Direction, normal) < 0f, Vector2.Zero);
	}
}