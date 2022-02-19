using JetBrains.Annotations;
using RayTracer.Core.Environment;
using RayTracer.Core.Graphics;
using RayTracer.Core.Hittables;
using RayTracer.Core.Materials;
using System.Numerics;
using static RayTracer.Core.Colour;
using static System.Numerics.Vector3;

namespace RayTracer.Core.Scenes;

/// <summary>
///  Static class that contains a list of pre-made scenes
/// </summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static class BuiltinScenes
{
	/// <summary>
	///  Simple scene with a single sphere at (0, 0, 0)
	/// </summary>
	public static readonly Scene Sphere = new(
			"Sphere Scene", new Camera(new Vector3(0, 0, 2), Zero, UnitY, 20, 16f / 9f), new SceneObject[]
			{
					new("Sphere", new Sphere(Zero, .1f), new DiffuseMaterial(Red))
			},
			new DefaultSkyBox()
	);

	/// <summary>
	///  Simple scene with two spheres at (+-1, 0, 0)
	/// </summary>
	public static readonly Scene ThreeSpheres = new(
			"Three Spheres", new Camera(new Vector3(0, 1, 5), Zero, UnitY, 5, 16f / 9f), new SceneObject[]
			{
					new("Sphere 1", new Sphere(new Vector3(0.1f),  .1f), new DiffuseMaterial(Lerp(Red,   White, 0.1f))),
					new("Sphere 2", new Sphere(new Vector3(0),     .1f), new DiffuseMaterial(Lerp(Green, White, 0.8f))),
					new("Sphere 3", new Sphere(new Vector3(-0.1f), .1f), new DiffuseMaterial(Lerp(Blue,  White, 0.1f)))
			},
			new DefaultSkyBox()
	);

	/// <summary>
	///  Simple scene with two spheres at (+-1, 0, 0)
	/// </summary>
	public static readonly Scene WithGround = new(
			"With Ground", new Camera(new Vector3(0, 1, -5), Zero, UnitY, 20, 16f / 9f), new SceneObject[]
			{
					new("Sphere", new Sphere(new Vector3(0, 1f,     0), 1f), new DiffuseMaterial(Green)),
					new("Ground", new Sphere(new Vector3(0, -1000f, 0), 1000f), new DiffuseMaterial(0.5f * White))
			},
			new DefaultSkyBox()
	);
}