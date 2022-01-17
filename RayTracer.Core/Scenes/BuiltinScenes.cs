using RayTracer.Core.Graphics;
using RayTracer.Core.Meshes;
using System.Numerics;

namespace RayTracer.Core.Scenes;

/// <summary>
/// Static class that contains a list of pre-made scenes
/// </summary>
public static class BuiltinScenes
{
	/// <summary>
	/// Simple scene with a single sphere at (0, 0, 0)
	/// </summary>
	public static readonly Scene Sphere = new("Sphere Scene", new Camera(-Vector3.UnitZ, Vector3.Zero, Vector3.UnitY, 0,90,16f/9f, 1),new SceneObject[]
	{
			new("Sphere", new Sphere{Centre = Vector3.Zero, Radius = .1f})
	});

	/// <summary>
	/// Simple scene with two spheres at (+-1, 0, 0)
	/// </summary>
	public static readonly Scene TwoSpheres = new("Two Spheres",new Camera(-Vector3.UnitZ, Vector3.Zero, Vector3.UnitY, 0, 90, 16f/9f, 1),new SceneObject[]
	{
			new("Sphere 1", new Sphere {Centre = Vector3.UnitX, Radius  = .1f}),
			new("Sphere 2", new Sphere {Centre = -Vector3.UnitX, Radius = .1f}),
	});
}