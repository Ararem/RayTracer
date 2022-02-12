using OLDRayTracer.Materials;
using RayTracer.Core.Environment;
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
					new("Sphere", new Sphere { Centre = Vector3.Zero, Radius = .1f }, new DiffuseMaterial(Colour.Red))
			},
			new DefaultSkyBox()
	);

	/// <summary>
	///  Simple scene with two spheres at (+-1, 0, 0)
	/// </summary>
	public static readonly Scene TwoSpheres = new(
			"Two Spheres", new Camera(new Vector3(0, 0, 2), Vector3.Zero, Vector3.UnitY, 20, 16f / 9f), new SceneObject[]
			{
					new("Sphere 1", new Sphere { Radius = .1f, Centre = +0.2f * Vector3.One }, new DiffuseMaterial(Colour.Red   + (0.5f * Colour.White))),
					new("Sphere 2", new Sphere { Radius = .1f, Centre = 0f    * Vector3.One }, new DiffuseMaterial(Colour.Green + (0.5f * Colour.White))),
					new("Sphere 3", new Sphere { Radius = .1f, Centre = -0.2f * Vector3.One }, new DiffuseMaterial(Colour.Blue  + (0.5f * Colour.White)))
			},
			new DefaultSkyBox()
	);
}