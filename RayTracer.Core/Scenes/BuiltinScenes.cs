using JetBrains.Annotations;
using RayTracer.Core.Environment;
using RayTracer.Core.Graphics;
using RayTracer.Core.Hittables;
using RayTracer.Core.Materials;
using System.Numerics;
using System.Reflection;
using static RayTracer.Core.Colour;
using static RayTracer.Core.Rand;
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
			"A lonely Sphere", new Camera(new Vector3(0, 0, 2), new Vector3(0.1f, 0f, 0f), UnitY, 20, 16f / 9f, 0, 1f), new SceneObject[]
			{
					new("Sphere", new Sphere(Zero, .1f), new StandardMaterial(Red, Black, 1f))
			},
			new DefaultSkyBox()
	);

	/// <summary>
	///  Simple scene with two spheres at (+-1, 0, 0)
	/// </summary>
	public static readonly Scene RgbSpheres = new(
			"RGB Spheres", new Camera(new Vector3(0, 0, 5), Zero, UnitY, 5, 16f / 9f, 2f, 5f), new SceneObject[]
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
			"With Ground", new Camera(new Vector3(0, 1, -5), Zero, UnitY, 20, 16f / 9f, 2f, 7f), new SceneObject[]
			{
					new("Sphere", new Sphere(new Vector3(0, 1f, 0), 1f), new StandardMaterial(Green,                                                         Black, 1f)),
					new("Ground", new XzPlane(NegativeInfinity, PositiveInfinity, NegativeInfinity, PositiveInfinity, 0), new StandardMaterial(0.5f * White, Black, 1f))
			},
			new DefaultSkyBox()
	);

	/// <summary>
	///  Cover for RayTracing in a weekend, chapter one
	/// </summary>
	public static readonly Scene RtInAWeekendCover1;

	static BuiltinScenes()
	{
		//RayTracing in a Weekend Chapter 1 cover
		{
			List<SceneObject> objects = new()
			{
					new SceneObject("Ground", new XzPlane(NegativeInfinity, PositiveInfinity, NegativeInfinity, PositiveInfinity, -1f), new StandardMaterial(new Colour(0.5f), Black, 1f))
					// new SceneObject("Ground", new Sphere(new Vector3(0,-1001f, 0), 1000f), new StandardMaterial(new Colour(0.5f), Black, 1f))
			};
			for (int a = -11; a < 11; a++)
			{
				for (int b = -11; b < 11; b++)
				{
					float   chooseMat = RandomFloat();
					Vector3 center    = new(a + (0.9f * RandomFloat()), 0.2f, b + (0.9f * RandomFloat()));

					if ((center - new Vector3(4f, 0.2f, 0f)).Length() > 0.9)
					{
						Material sphereMaterial;

						if (chooseMat < 0.8)
						{
							// diffuse
							Colour albedo = new(RandomFloat());
							sphereMaterial = new StandardMaterial(albedo, Black, 1f);
						}
						else if (chooseMat < 0.95)
						{
							// metal
							Colour albedo = new(RandomFloat(0.5f, 1f));
							float  fuzz   = RandomFloat(0f, 0.5f);
							sphereMaterial = new StandardMaterial(albedo, Black, 1 - fuzz);
						}
						else
						{
							// glass
							sphereMaterial = new RefractiveMaterial(RandomFloat(0.00001f, 50f), Lerp(White, Blue, 0.2f));
						}

						objects.Add(new SceneObject($"Sphere ({a},{b})", new Sphere(center, 0.2f), sphereMaterial));
					}
				}
			}

			objects.Add(new SceneObject("Sphere A", new Sphere(new Vector3(0,  1, 0), 1), new RefractiveMaterial(1.5f, White)));
			objects.Add(new SceneObject("Sphere B", new Sphere(new Vector3(-4, 1, 0), 1), new StandardMaterial(new Colour(.4f, .2f, .1f), Black, 1f)));
			objects.Add(new SceneObject("Sphere C", new Sphere(new Vector3(4,  1, 0), 1), new StandardMaterial(new Colour(.7f, .6f, .5f), Black, 0f)));
			RtInAWeekendCover1 = new Scene("RayTracing Chapter 1", new Camera(new Vector3(13, 2, 3), Zero, UnitY, 20, 16f / 9f, 0.1f, 10f), objects.ToArray(), new DefaultSkyBox());
		}
	}

	/// <summary>
	///  Gets all the builtin scenes
	/// </summary>
	public static IEnumerable<Scene> GetAll()
	{
		return typeof(BuiltinScenes)
				.GetFields(BindingFlags.Public | BindingFlags.Static)
				.Where(f => f.FieldType == typeof(Scene))
				.Select(f => (Scene)f.GetValue(null)!);
	}
}