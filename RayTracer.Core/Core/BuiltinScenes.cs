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
	///  Fancy scene containing (hopefully) every type of shape
	/// </summary>
	public static Scene Everything
	{
		get
		{
			List<SceneObject> objects = new();
			List<Light>       lights  = new();
			Camera            camera  = Camera.Create(new Vector3(0, 3, 7), UnitY * 3, UnitY, 70, 16f / 9f, 0f, 1f);

			//Ground plane
			{
				objects.Add(new SceneObject("Ground", new InfinitePlane(Zero, UnitY), new StandardMaterial(HalfGrey, Black, .5f)));
			}
			//Axis aligned planes
			{
				Vector3  low = new(-7, 0, -2), high = new(-5, 3f, -.5f);
				objects.Add(new SceneObject("XY", new XYPlane(low.X, high.X, low.Y, high.Y, low.Z), new StandardMaterial(new Colour(1f, .5f,.5f), Black, .5f)));
				objects.Add(new SceneObject("YZ", new YZPlane(low.Y, high.Y, low.Z, high.Z, low.X), new StandardMaterial(new Colour(.5f, 1f,.5f), Black, .5f)));
				objects.Add(new SceneObject("XZ", new XZPlane(low.X, high.X, low.Z, high.Z, low.Y), new StandardMaterial(new Colour(.5f, .5f,1f), Black, .5f)));
			}
			//Sphere, inside planes
			{
				objects.Add(new SceneObject("Sphere", new Sphere(new Vector3(-6, 3f, -1.25f) ,.7f), new RefractiveMaterial(RefractiveMaterial.GlassIndex, White)));
			}

			return new Scene("EVERYTHING!!!", camera, objects.ToArray(), lights.ToArray(), new DefaultSkyBox());
		}
	}

	/// <summary>
	///  Simple scene with a single sphere at (0, 0, 0)
	/// </summary>
	public static Scene Sphere => new(
			"A lonely Sphere", Camera.Create(new Vector3(0, 0, 2), new Vector3(0.1f, 0f, 0f), UnitY, 20, 16f / 9f, 0, 1f), new SceneObject[]
			{
					new("Sphere", new Sphere(Zero, .1f), new StandardMaterial(Red, Black, 1f))
			},
			Array.Empty<Light>(),
			new DefaultSkyBox()
	);

	/// <summary>
	///  Simple scene with two spheres at (+-1, 0, 0)
	/// </summary>
	public static Scene RgbSpheres => new(
			"RGB Spheres", Camera.Create(new Vector3(0, 0, 5), Zero, UnitY, 5, 16f / 9f, .00002f, 5f), new SceneObject[]
			{
					new("Sphere 1", new Sphere(new Vector3(0.1f),  .1f), new StandardMaterial(Lerp(Red,   White, 0.5f), Black, 1f)),
					new("Sphere 2", new Sphere(new Vector3(0),     .1f), new StandardMaterial(Lerp(Green, White, 0.5f), Black, 1f)),
					new("Sphere 3", new Sphere(new Vector3(-0.1f), .1f), new StandardMaterial(Lerp(Blue,  White, 0.5f), Black, 1f))
					// new("Plane", new InfinitePlane(Zero, UnitZ), new RefractiveMaterial(1, White * .5f, Black))
			},
			Array.Empty<Light>(),
			new DefaultSkyBox()
	);

	/// <summary>
	///  The good 'ol cornell box, as is traditional for raytracing
	/// </summary>
	public static Scene CornellBox
	{
		get
		{
			Material greyWallMaterial = new StandardMaterial(new Colour(0.73f, 0.73f, 0.73f), Black, 1f);
			return new Scene(
					"Cornell Box", Camera.Create(new Vector3(278, 278, -800), new Vector3(278, 278, 0), UnitY, 40f, 1f / 1f, 0f, 1f), new SceneObject[]
					{
							new("Left", new YZPlane(0,  555, 0, 555, 0), new StandardMaterial(new Colour(0.5f,   0.1f, 0.1f), Black, 1f)),
							new("Right", new YZPlane(0, 555, 0, 555, 555), new StandardMaterial(new Colour(0.1f, 0.5f, 0.1f), Black, 1f)),
							new("Back", new XYPlane(0, 555, 0, 555, 555), greyWallMaterial),
							new("Top", new XZPlane(0,    555, 0, 555, 555), greyWallMaterial),
							new("Bottom", new XZPlane(0, 555, 0, 555, 0), greyWallMaterial),

							new("Light", new XZPlane(213, 343, 227, 332, 554.9f), new StandardMaterial(White, White, 1f)),

							new("Small Box", new Box(Matrix4x4.CreateScale(165, 165, 165) * Matrix4x4.CreateFromYawPitchRoll(-18 * (MathF.PI / 180f), 0 * (MathF.PI / 180f), 0 * (MathF.PI / 180f)) * Matrix4x4.CreateTranslation(212.5f, 82.5f, 147.5f)), new StandardMaterial(new Colour(0.73f, 0.73f, 0.73f), Black, 1f)),
							new("Tall Box", new Box(Matrix4x4.CreateScale(165,  330, 165) * Matrix4x4.CreateFromYawPitchRoll(15  * (MathF.PI / 180f), 0 * (MathF.PI / 180f), 0 * (MathF.PI / 180f)) * Matrix4x4.CreateTranslation(347.5f, 165f,  377.5f)), new StandardMaterial(new Colour(0.73f, 0.73f, 0.73f), Black, 1f))
							// new("Tall Box", new ConstantDensityMedium(new Box(Matrix4x4.CreateScale(165, 330, 165) * Matrix4x4.CreateFromYawPitchRoll(15 * (MathF.PI / 180f), 0 * (MathF.PI / 180f), 0 * (MathF.PI / 180f)) * Matrix4x4.CreateTranslation(347.5f, 165f, 377.5f)), 0.01f), new VolumetricMaterial(new Colour(0.73f, 0.73f, 0.73f)))
							// new ("Small Box Sphere", new Sphere(new Vector3(212.5f, 265f, 147.5f), 100), new EmissiveRefractiveMaterial(RefractiveMaterial.GlassIndex, White, Blue)),
							// new ("Tall Box Sphere", new Sphere(new Vector3(347.5f,  430f, 377.5f), 100), new RefractiveMaterial(RefractiveMaterial.GlassIndex, White)),
					},
					new Light[]
					{
							//Centre ceiling light
							new DiffuseSphereLight(new Vector3((213 + 343) / 2f, 554 - 50, (227 + 332) / 2f), 40, White * 0.5f, 150, 1.5f)
					},
					new SingleColourSkyBox(Black)
			);
		}
	}

	/// <summary>
	///  Testing scene
	/// </summary>
	public static Scene Testing => new(
			"Testing", Camera.Create(new Vector3(0f, 0.0f, 1.5f), new Vector3(.0f, .0f, 0), UnitY, 90, 16f / 9f, 0f, 7f), new SceneObject[]
			{
					new(
							"Test Object",
							new Quad(Zero, UnitY + new Vector3(.5f, 0, 0), UnitX - UnitY),
							new StandardMaterial(HalfGrey, Black, .0f)
					),
					new(
							"UnitX",
							new Sphere(UnitX, .05f),
							new StandardMaterial(Red, Black, .0f)
					),
					new(
							"UnitY",
							new Sphere(UnitY, .15f),
							new StandardMaterial(Green, Black, .0f)
					),
					new(
							"Origin",
							new Sphere(Zero, .1f),
							new StandardMaterial(Blue, Black, .0f)
					)
			},
			Array.Empty<Light>(),
			new DefaultSkyBox()
	);

	/// <summary>
	///  Cover for RayTracing in a weekend, chapter one
	/// </summary>
	public static Scene RtInAWeekendCover1
	{
		get
		//RayTracing in a Weekend Chapter 1 cover
		{
			List<SceneObject> objects = new();
			List<Light>       lights  = new();
			for (int a = -11; a < 11; a++)
			{
				for (int b = -11; b < 11; b++)
				{
					float   chooseMat = RandomFloat01();
					Vector3 center    = new(a + (0.9f * RandomFloat01()), 0.2f, b + (0.9f * RandomFloat01()));

					if ((center - new Vector3(4f, 0.2f, 0f)).Length() > 0.9)
					{
						Material sphereMaterial;

						if (chooseMat < 0.3)
						{
							// diffuse
							Colour albedo = RandomColour(Black, White);
							sphereMaterial = new StandardMaterial(albedo, Black, 1f);
						}
						else if (chooseMat < 0.5)
						{
							// diffuse
							Colour albedo = RandomColour(Black, White);
							sphereMaterial = new StandardMaterial(White, albedo, 1f);
						}
						else if (chooseMat < 0.65)
						{
							// metal
							Colour albedo = RandomColour(Black, White);
							float  fuzz   = RandomFloat(0f, 0.5f);
							sphereMaterial = new StandardMaterial(albedo, Black, 1 - fuzz);
						}
						else if (chooseMat < 0.655)
						{
							Colour             colour = RandomColour(HalfGrey, White);
							SurfaceSphereLight light  = new(center, 0.4f, colour, 1f);
							lights.Add(light);
							sphereMaterial = new StandardMaterial(White, Black, 0f);
						}
						else
						{
							// glass
							Colour tint = RandomColour(Black, White);
							sphereMaterial = new RefractiveMaterial(RandomFloat(1f, 5f), tint);
						}

						objects.Add(new SceneObject($"Sphere ({a},{b})", new Sphere(center, 0.2f), sphereMaterial));
					}
				}
			}

			objects.Add(new SceneObject("Sphere A", new Sphere(new Vector3(0,  1, 0), 1), new RefractiveMaterial(1.5f, White)));
			objects.Add(new SceneObject("Sphere B", new Sphere(new Vector3(-4, 1, 0), 1), new StandardMaterial(new Colour(.4f, .2f, .1f), Black, 1f)));
			objects.Add(new SceneObject("Sphere C", new Sphere(new Vector3(4,  1, 0), 1), new StandardMaterial(new Colour(.7f, .6f, .5f), Black, 0f)));
			objects.Add(new SceneObject("Ground",   new InfinitePlane(Zero, UnitY),       new StandardMaterial(new Colour(0.5f),          Black, 1f)));
			return new Scene("RayTracing Chapter 1", Camera.Create(new Vector3(13, 2, 3), Zero, UnitY, 20, 16f / 9f, 0f, 10f), objects.ToArray(), lights.ToArray(), new DefaultSkyBox());
		}
	}

	/// <summary>
	///  Gets all the builtin scenes
	/// </summary>
	public static IEnumerable<Scene> GetAll()
	{
		return typeof(BuiltinScenes)
				.GetProperties(BindingFlags.Public | BindingFlags.Static)
				.Where(p => p.PropertyType == typeof(Scene))
				.Select(p => (Scene)p.GetValue(null)!);
	}
}