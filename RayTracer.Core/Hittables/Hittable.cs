using RayTracer.Core.Graphics;

namespace RayTracer.Core.Hittables;

/// <summary>
/// Base class for a hittable. Represents the surface/structure of a render-able object.
/// </summary>
public abstract class Hittable
{
	public abstract float Hit(Ray ray);
}