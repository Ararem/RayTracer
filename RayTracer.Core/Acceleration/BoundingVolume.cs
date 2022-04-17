namespace RayTracer.Core.Acceleration;

/// <summary>
/// Base class for implementing a bounding box.
/// </summary>
public abstract record BoundingVolume
{

	/// <summary>
	/// Tries to see if the given ray will intersect with the bounding box or not.
	/// </summary>
	/// <param name="ray">The ray to check for intersections along</param>
	/// <param name="kMin">Lower bound for distance along the ray</param>
	/// <param name="kMax">Upper bound for distance along the ray</param>
	/// <returns><see langword="true"/> if the ray intersects this bounding volume and may hit the object inside, else <see langword="false"/></returns>
	public abstract bool Hit(Ray ray, float kMin, float kMax);
}