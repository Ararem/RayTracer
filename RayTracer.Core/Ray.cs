using JetBrains.Annotations;
using System.Numerics;

namespace RayTracer.Core;

/// <summary>A 3-dimensional ray, starting at the <see cref="Origin"/> and going in a certain <see cref="Direction"/></summary>
/// <remarks>
///  The <see cref="Direction"/> of the ray will always be normalised upon calling the default constructor.
/// </remarks>
[PublicAPI]
public readonly struct Ray
{
	/// <summary>A 3-dimensional ray, starting at the <see cref="Origin"/> and going in a certain <see cref="Direction"/></summary>
	/// <param name="origin">The point in space where the ray starts</param>
	/// <param name="direction">The direction to move in when progressing along the ray. Will be normalized</param>
	public Ray(Vector3 origin, Vector3 direction)
	{
		Origin    = origin;
		Direction = Vector3.Normalize(direction);
	}

	/// <summary>Gets the point a certain distance along this ray</summary>
	/// <param name="k">How far down the ray the point should be</param>
	/// <returns></returns>
	public Vector3 PointAt(float k) => Origin + (k * Direction);

	/// <summary>Gets a ray that joins two points. The direction of the ray is <paramref name="p1"/> => <paramref name="p2"/></summary>
	public static Ray FromPoints(Vector3 p1, Vector3 p2) => new(p1,p2 - p1);

	/// <summary>The point in space where the ray starts</summary>
	public Vector3 Origin { get; }

	/// <summary>The direction to move in when progressing along the ray</summary>
	public Vector3 Direction { get; }
}