using RayTracer.Core.Graphics;
using RayTracer.Core.Hittables;
using System.Numerics;

namespace RayTracer.Core.Scenes;

/// <summary>
///  Static class that contains a list of pre-made scenes
/// </summary>
public static class BuiltinScenes
{
	/// <summary>
	///  Simple scene with a single sphere at (0, 0, 0)
	/// </summary>
	public static readonly Scene Sphere = new(
			"Sphere Scene", new Camera(new Vector3(0, 0, 2), Vector3.Zero, Vector3.UnitY, 20, 16f / 9f), new SceneObject[]
			{
					new("Sphere", Vector3.UnitX * 0.0f, new Sphere { Radius = .1f })
			}
	);

	/// <summary>
	///  Simple scene with two spheres at (+-1, 0, 0)
	/// </summary>
	public static readonly Scene TwoSpheres = new(
			"Two Spheres", new Camera(new Vector3(0, 0, 2), Vector3.Zero, Vector3.UnitY, 20, 16f / 9f), new SceneObject[]
			{
					new("Sphere 1", Vector3.Zero, new Sphere { Radius = .1f }),
					new("Sphere 2", Vector3.Zero, new Sphere { Radius = .1f }),
					new("Sphere 3", Vector3.Zero, new Sphere { Radius = .1f })
			}
	);
}