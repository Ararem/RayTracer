using JetBrains.Annotations;
using RayTracer.Core.Environment;
using RayTracer.Core.Graphics;
using RayTracer.Core.Hittables;
using RayTracer.Core.Materials;
using System.Numerics;
using System.Reflection;
using static RayTracer.Core.Colour;
using static System.Numerics.Vector3;
using static System.Single;

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
			"A lonely Sphere", new Camera(new Vector3(0, 0, 2), Zero, UnitY, 20, 16f / 9f), new SceneObject[]
			{
					new("Sphere", new Sphere(Zero, .1f), new StandardMaterial(Red, Black, 1f))
			},
			new DefaultSkyBox()
	);

	/// <summary>
	///  Simple scene with two spheres at (+-1, 0, 0)
	/// </summary>
	public static readonly Scene RgbSpheres = new(
			"RGB Spheres", new Camera(new Vector3(0, 1, 5), Zero, UnitY, 5, 16f / 9f), new SceneObject[]
			{
					new("Sphere 1", new Sphere(new Vector3(0.1f),  .1f), new StandardMaterial(Lerp(Red,   White, 0.5f), Black, 1f)),
					new("Sphere 2", new Sphere(new Vector3(0),     .1f), new StandardMaterial(Lerp(Green, White, 0.5f), Black, 1f)),
					new("Sphere 3", new Sphere(new Vector3(-0.1f), .1f), new StandardMaterial(Lerp(Blue,  White, 0.5f), Black, 1f))
			},
			new DefaultSkyBox()
	);

	/// <summary>
	///  Simple scene with a sphere at (+-1, 0, 0) and a ground plane
	/// </summary>
	public static readonly Scene WithGround = new(
			"With Ground", new Camera(new Vector3(0, 1, -5), Zero, UnitY, 20, 16f / 9f), new SceneObject[]
			{
					new("Sphere", new Sphere(new Vector3(0, 1f, 0), 1f), new StandardMaterial(Green,                                                         Black, 1f)),
					new("Ground", new XzPlane(NegativeInfinity, PositiveInfinity, NegativeInfinity, PositiveInfinity, 0), new StandardMaterial(0.5f * White, Black, 1f))
			},
			new DefaultSkyBox()
	);

	/// <summary>
	/// Gets all the builtin scenes
	/// </summary>
	public static IEnumerable<Scene> GetAll()
	{
		return typeof(BuiltinScenes)
				.GetFields(BindingFlags.Public | BindingFlags.Static)
				.Where(f => f.FieldType == typeof(Scene))
				.Select(f => (Scene)f.GetValue(null)!);
	}
}