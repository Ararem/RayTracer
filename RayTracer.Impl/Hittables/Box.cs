using RayTracer.Core.Acceleration;
using System.Numerics;
using static System.MathF;
using static System.Numerics.Vector3;

namespace RayTracer.Core.Hittables;

/// <summary>
///  Represents a 3-dimensional box.
/// </summary>
/// <remarks>
///  This box is unusual in the sense that it has a <see cref="Matrix4x4"/> to contain rotation, translation and scaling, instead of separate parameters
///  for each. In order to position a <see cref="Box"/>, use the <see cref="Matrix4x4"/><c>.CreateXXX</c> methods (like
///  <see cref="Matrix4x4.CreateScale(System.Numerics.Vector3)"/>). When combining these matrices, make sure to multiply them in the order
///  <c>Translate * Rotate * Scale</c>, or you may get unintended results (see
///  <a href="https://gamedev.stackexchange.com/questions/29260/transform-matrix-multiplication-order/29265#29265">this StackOverflow answer</a>
///  )
/// </remarks>
/// <param name="BoxToWorldTransform">Matrix to transform box-space to world-space</param>
/// <param name="WorldToBoxTransform">Matrix to transform world-space to box-space</param>
public record Box(Matrix4x4 BoxToWorldTransform, Matrix4x4 WorldToBoxTransform) : Hittable
{
	/// <inheritdoc/>
	public override AxisAlignedBoundingBox BoundingVolume {
		get
		{
			//Bounding volume calculations
			//How i do this is I calculate where each of the corners will end up in world-space, and then create an AABB around them
			//It's not super efficient but it should be pretty fast and simple
			//Due to how IQ implemented his code, valid box-space is [-1..1] so just plug these coords into the matrix and we find where they will end up
			Vector3[] corners =
			{
					new(-1, -1, -1),
					new(-1, -1, 1),
					new(-1, 1, -1),
					new(-1, 1, 1),
					new(1, -1, -1),
					new(1, -1, 1),
					new(1, 1, -1),
					new(1, 1, 1)
			};
			//Transform each of the corners by our box to world matrix
			for (int i = 0; i < corners.Length; i++) corners[i] = Transform(corners[i], BoxToWorldTransform);
			AxisAlignedBoundingBox boundingVolume               = AxisAlignedBoundingBox.Encompass(corners);
			return boundingVolume;
		}}

	/// <summary>
	///  Creates a <see cref="Box"/> from two opposing corners
	/// </summary>
	public static Box CreateFromCorners(Vector3 corner1, Vector3 corner2)
	{
		corner1 = Min(corner1, corner2);
		corner2 = Max(corner1, corner2);
		Vector3 size   = corner2 - corner1;
		Vector3 centre = (corner2 + corner1) / 2f;

		Matrix4x4 matrix = Matrix4x4.CreateScale(size) * Matrix4x4.CreateTranslation(centre);
		return Create(matrix);
	}

	/// <summary>
	///  Creates a box from a transform matrix
	/// </summary>
	/// <param name="boxToWorldTransform">The matrix used to create the box</param>
	/// <exception cref="ArgumentOutOfRangeException">The matrix used is non-invertible</exception>
	public static Box Create(Matrix4x4 boxToWorldTransform)
	{
		if (!Matrix4x4.Invert(boxToWorldTransform, out Matrix4x4 worldToBoxTransform)) throw new ArgumentOutOfRangeException(nameof(boxToWorldTransform), boxToWorldTransform, "Matrix not invertible");

		return new Box (boxToWorldTransform, worldToBoxTransform);
	}

	/// <inheritdoc/>
	public override HitRecord? TryHit(Ray ray, float kMin, float kMax)
	{
		/*
		 * Modified version of IQ's code here, many thanks!!
		 * Some notes on what I did:
		 * 1. Changed the names to be a lot more clear for the inputs and outputs
		 * 2. Refactored it a bit to match what I needed to return and take in
		 * 3. A few changes because OpenGL has features C# doesn't
		 * 4. Add some NaN and range checks, since my raytracer doesn't like those very much
		 * 5. (Important) I've combined everything into one input matrix, meaning the `rad`/`halfLengths` vector no longer exists, as it's contained in the matrix transform now
		 */

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
		Vector3 m = new Vector3(1f) / rd;
		//To be honest I really dislike having this, but when it's left in it causes weird graphical artifacts and causes a whole bunch of NaN's down the line, screwing everything up
		if (m.X is float.NaN || m.Y is float.NaN || m.Z is float.NaN || float.IsInfinity(m.X) || float.IsInfinity(m.Y) || float.IsInfinity(m.Z)) return null;
		Vector3 s = new(
				rd.X < 0f ? 1f : -1f,
				rd.Y < 0f ? 1f : -1f,
				rd.Z < 0f ? 1f : -1f
		);
		Vector3 t1 = m * (-ro + (s / 2f));
		Vector3 t2 = m * (-ro - (s / 2f));

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
					BoxToWorldTransform.M11,
					BoxToWorldTransform.M12,
					BoxToWorldTransform.M13
			);
			normal    = v * s.X;
			uv        = new Vector2(ro.Y, ro.Z) + (new Vector2(rd.Y, rd.Z) * t1.X);
			faceIndex = (1 + (int)s.X) / 2;
		}
		else if (t1.Y > t1.Z)
		{
			Vector3 v = new(
					BoxToWorldTransform.M21,
					BoxToWorldTransform.M22,
					BoxToWorldTransform.M23
			);
			normal    = v * s.Y;
			uv        = new Vector2(ro.Z, ro.Z) + (new Vector2(rd.Z, rd.X) * t1.Y);
			faceIndex = (5 + (int)s.Y) / 2;
		}
		else
		{
			Vector3 v = new(
					BoxToWorldTransform.M31,
					BoxToWorldTransform.M32,
					BoxToWorldTransform.M33
			);
			normal    = v * s.Z;
			uv        = new Vector2(ro.X, ro.Y) + (new Vector2(rd.X, rd.Y) * t1.Z);
			faceIndex = (9 + (int)s.Z) / 2;
		}

		Vector3 worldPoint = ray.PointAt(k);
		//Transform the point from world space to box space to get the local point
		Vector3 localPoint = Transform(worldPoint, WorldToBoxTransform);

		//Will implement these later
		_ = uv;
		_ = faceIndex;

		//TODO: UV's
		//Side note: UV's are completely messed up
		//X ranges approx [-0.71..+0.77], while Y ranges ~~ [-0.7..2.2]????
		//Don't ask me how the hell that works, I don't know, but I know that something is broken and I can't be bothered to fix it, so I'm just disabling UV's
		return new HitRecord(ray, worldPoint, localPoint, Normalize(normal), k, Dot(ray.Direction, normal) < 0f, Vector2.Zero);
	}

	/// <inheritdoc/>
	public override bool FastTryHit(Ray ray, float kMin, float kMax)
	{
		//Same as TryHit() but with HitRecord calculation code removed

		// convert from world to box space
		Vector4 temp4 = Vector4.Transform(new Vector4(ray.Direction, 0f), WorldToBoxTransform);
		Vector3 rd    = new(temp4.X, temp4.Y, temp4.Z);
		temp4 = Vector4.Transform(new Vector4(ray.Origin, 1f), WorldToBoxTransform);
		Vector3 ro = new(temp4.X, temp4.Y, temp4.Z);

		// ray-box intersection in box space
		Vector3 m = new Vector3(1f) / rd;
		//To be honest I really dislike having this, but when it's left in it causes weird graphical artifacts and causes a whole bunch of NaN's down the line, screwing everything up
		if (m.X is float.NaN || m.Y is float.NaN || m.Z is float.NaN || float.IsInfinity(m.X) || float.IsInfinity(m.Y) || float.IsInfinity(m.Z)) return false;
		Vector3 s = new(
				rd.X < 0f ? 1f : -1f,
				rd.Y < 0f ? 1f : -1f,
				rd.Z < 0f ? 1f : -1f
		);
		Vector3 t1 = m * (-ro + (s / 2f));
		Vector3 t2 = m * (-ro - (s / 2f));

		float kNear = Max(Max(t1.X, t1.Y), t1.Z);
		float kFar  = Min(Min(t2.X, t2.Y), t2.Z);

		//Validate our K value ranges
		if ((kNear > kFar) || (kFar < 0f)) return false;
		float k = kNear;
		if ((k < kMin) || (kMax < k))
		{
			k = kFar;
			if ((k < kMin) || (kMax < k)) return false;
		}

		if (float.IsNaN(k)) return false;

		return true;
	}
}