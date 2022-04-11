using JetBrains.Annotations;
using RayTracer.Core.Environment;
using RayTracer.Core.Hittables;
using RayTracer.Core.Materials;
using System.Numerics;
using System.Reflection;
using static RayTracer.Core.Colour;
using static RayTracer.Core.RandUtils;
using static System.Numerics.Vector3;

namespace RayTracer.Core;

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
					new("Sphere 1", new Sphere(new Vector3(0.1f),  .15f), new StandardMaterial(Lerp(Red,   White, 0.5f), Black, 1f)),
					new("Sphere 2", new Sphere(new Vector3(0),     .15f), new StandardMaterial(Lerp(Green, White, 0.5f), Black, 1f)),
					new("Sphere 3", new Sphere(new Vector3(-0.1f), .15f), new StandardMaterial(Lerp(Blue,  White, 0.5f), Black, 1f)),
					new("Plane", new InfinitePlane(Zero, UnitZ), new RefractiveMaterial(1, White * .5f, Black))
			},
			new DefaultSkyBox()
	);

	/// <summary>
	///  Simple scene with a sphere at (+-1, 0, 0) and a ground plane
	/// </summary>
	public static readonly Scene WithGround = new(
			"With Ground", new Camera(new Vector3(0, 1, -5), Zero, UnitY, 90, 16f / 9f, 0f, 7f), new SceneObject[]
			{
					new("Sphere", new Sphere(new Vector3(0, 0.5f, 0), 1f), new StandardMaterial(Green,                       Black,      1f)),
					new("Ground", new Disk(Zero, Normalize(new Vector3(0, 1, -1)), 1.5f), new StandardMaterial(0.5f * White, Red * 0.1f, 1f))
			},
			new DefaultSkyBox()
	);

	/// <summary>
	///  Testing scene
	/// </summary>
	public static readonly Scene Testing = new(
			"Testing", new Camera(new Vector3(0, 0, -5), Zero, UnitY, 90, 16f / 9f, 0f, 7f), new SceneObject[]
			{
					// new("Object", new Cylinder(-One, One, 1f), new StandardMaterial(White * 0.5f, Black, .1f))
					// new ("HexBox", new HexPlaneBox(Zero, new Vector3(1,.2f, 1)), new StandardMaterial(HalfGrey, Black, 1f))
					new(
							"Box",
							new Box(
									-One, One,
									Matrix4x4.CreateFromYawPitchRoll(0 *(MathF.PI /180f), 0 *(MathF.PI /180f), 0 *(MathF.PI /180f))
							),
							new StandardMaterial(HalfGrey, Black, 1f)
					)
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
			List<SceneObject> objects = new();
			for (int a = -11; a < 11; a++)
			{
				for (int b = -11; b < 11; b++)
				{
					float   chooseMat = RandomFloat01();
					Vector3 center    = new(a + (0.9f * RandomFloat01()), 0.2f, b + (0.9f * RandomFloat01()));

					if ((center - new Vector3(4f, 0.2f, 0f)).Length() > 0.9)
					{
						Material sphereMaterial;

						if (chooseMat < 0.6)
						{
							// diffuse
							Colour albedo = RandomColour(Red, White);
							sphereMaterial = new StandardMaterial(albedo, Black, 1f);
						}
						else if (chooseMat < 0.75)
						{
							// metal
							Colour albedo = RandomColour(Green, White);
							float  fuzz   = RandomFloat(0f, 0.5f);
							sphereMaterial = new StandardMaterial(albedo, Black, 1 - fuzz);
						}
						else
						{
							// glass
							sphereMaterial = new RefractiveMaterial(RandomFloat(1f, 5f), Lerp(White, Blue, RandomFloat01()), Black);
						}

						objects.Add(new SceneObject($"Sphere ({a},{b})", new Sphere(center, 0.2f), sphereMaterial));
					}
				}
			}

			objects.Add(new SceneObject("Sphere A", new Sphere(new Vector3(0,  1, 0), 1), new RefractiveMaterial(1.5f, White, Black)));
			objects.Add(new SceneObject("Sphere B", new Sphere(new Vector3(-4, 1, 0), 1), new StandardMaterial(new Colour(.4f, .2f, .1f), Black, 1f)));
			objects.Add(new SceneObject("Sphere C", new Sphere(new Vector3(4,  1, 0), 1), new StandardMaterial(new Colour(.7f, .6f, .5f), Black, 0f)));
			objects.Add(new SceneObject("Ground",   new InfinitePlane(Zero, UnitY),       new StandardMaterial(new Colour(0.5f),          Black, 1f)));
			// objects.Add(new SceneObject("Ground",   new Sphere(-1000*UnitY, 1000),             new StandardMaterial(new Colour(0.5f),          Black, 1f)));
			RtInAWeekendCover1 = new Scene("RayTracing Chapter 1", new Camera(new Vector3(13, 2, 3), Zero, UnitY, 20, 16f / 9f, 0f, 10f), objects.ToArray(), new DefaultSkyBox());
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