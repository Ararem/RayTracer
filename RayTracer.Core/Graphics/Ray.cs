using System.Numerics;

namespace RayTracer.Core.Graphics;

/// <summary>
///  A 3-dimensional ray, starting at the <see cref="Origin"/> and going in a certain <see cref="Direction"/>
/// </summary>
/// <param name="Origin">The point in space where the ray starts</param>
/// <param name="Direction">The direction to move in when progressing along the ray</param>
/// <remarks>
///  Although it is assumed that the <see cref="Direction"/> is normalised, this may not be the case, so be sure to call <c>Vector3.Normalize()</c>
/// </remarks>
public readonly record struct Ray(Vector3 Origin, Vector3 Direction)
{
	/// <summary>
	///  Gets the point a certain distance along this ray
	/// </summary>
	/// <param name="k">How far down the ray the point should be</param>
	/// <returns></returns>
	public Vector3 PointAt(float k) => Origin + (k * Direction);

	/// <summary>
	///  Gets a ray that joins two points. The direction of the ray is <paramref name="p1"/> => <paramref name="p2"/>
	/// </summary>
	public static Ray FromPoints(Vector3 p1, Vector3 p2) => new(p1, Vector3.Normalize(p2 - p1));

	/// <summary>
	///  Returns a copy of the current ray with the <see cref="Direction"/> vector normalized
	/// </summary>
	public Ray WithNormalizedDirection() => new(Origin, Vector3.Normalize(Direction));
}