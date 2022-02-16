using RayTracer.Core.Graphics;

namespace RayTracer.Core.Hittables;

/// <summary>
///  Base class for a hittable. Represents the surface/structure of a render-able object.
/// </summary>
public abstract class HittableBase
{
	/// <summary>
	///  Attempts to intersect the current hittable instance with a <see cref="Ray"/>
	/// </summary>
	/// <param name="ray">The ray to check for intersection with</param>
	/// <param name="kMin">Minimum distance along the ray to check for intersections</param>
	/// <param name="kMax">Maximum distance along the ray to check for intersection</param>
	/// <returns>
	///  If the ray hit this instance, returns a <see cref="HitRecord"/> containing information about where the ray intersection occured, otherwise
	///  <see langword="null"/> if no intersection occured
	/// </returns>
	public abstract HitRecord? TryHit(Ray ray, float kMin, float kMax);

	/// <inheritdoc/>
	public override string ToString() => GetType().Name;
}