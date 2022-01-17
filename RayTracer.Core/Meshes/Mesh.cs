using RayTracer.Core.Graphics;

namespace RayTracer.Core.Meshes;

/// <summary>
/// Base class for a mesh. Represents the surface/structure of a render-able object.
/// </summary>
public abstract class Mesh
{
	public abstract bool Hit(Ray ray);
}