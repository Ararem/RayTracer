using JetBrains.Annotations;
using RayTracer.Core;
using RayTracer.Impl.Hittables;
using RayTracer.Impl.Lights;
using RayTracer.Impl.Materials;
using RayTracer.Impl.Skyboxes;
using RayTracer.Impl.Textures;
using System.Numerics;
using System.Reflection;
using static RayTracer.Core.Colour;
using static RayTracer.Core.RandUtils;
using static System.Numerics.Vector3;
using static System.MathF;

namespace RayTracer.Impl;

/// <summary>Static class that contains a list of pre-made scenes</summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static class BuiltinScenes
{
	/// <summary>Testing scene</summary>
	public static Scene Testing
	{
		get
		{
			Material greyWallMaterial = new StandardMaterial(new Colour(0.73f, 0.73f, 0.73f), 1f);
			return new Scene(
					"Testing", Camera.Create(new Vector3(278, 278, -800), new Vector3(278, 278, 0), UnitY, 40f, 1f / 1f, 0f, 1f), new SceneObject[]
					{
							new("Left", new YZPlane(0,  555, 0, 555, 0){Material = new StandardMaterial(new Colour(0.5f,   0.1f, 0.1f), 1f)}),
							new("Right", new YZPlane(0, 555, 0, 555, 555){Material = new StandardMaterial(new Colour(0.1f, 0.5f, 0.1f), 1f)}),
							new("Back", new XYPlane(0, 555, 0, 555, 555){Material = greyWallMaterial}),
							new("Top", new XZPlane(0,    555, 0, 555, 555){Material = greyWallMaterial}),
							new("Bottom", new XZPlane(0, 555, 0, 555, 0){Material = greyWallMaterial}),

							new("Small Box", new Box(Matrix4x4.CreateScale(165, 165, 165) * Matrix4x4.CreateFromYawPitchRoll(-18 * (PI / 180f), 0 * (PI / 180f), 0 * (PI / 180f)) * Matrix4x4.CreateTranslation(212.5f, 82.5f, 147.5f)), new StandardMaterial(new Colour(0.73f, 0.73f, 0.73f), 1f)),
							new("Tall Box", new Box(Matrix4x4.CreateScale(165,  330, 165) * Matrix4x4.CreateFromYawPitchRoll(15  * (PI / 180f), 0 * (PI / 180f), 0 * (PI / 180f)) * Matrix4x4.CreateTranslation(347.5f, 165f,  377.5f)), new StandardMaterial(new Colour(0.73f, 0.73f, 0.73f), 1f)),

							new("Glass", new Box(Matrix4x4.CreateScale(200) * Matrix4x4.CreateFromYawPitchRoll(15 * (PI / 180f), 0 * (PI / 180f), 0 * (PI / 180f)) * Matrix4x4.CreateTranslation(new Vector3(212.5f, 265f, 147.5f))), new RefractiveMaterial(3f, new SolidColourTexture(new Colour(0.8f, 0.9f, 1f)), false)),

							// new("Small Box Sphere", new Sphere(new Vector3(212.5f, 265f, 147.5f), 200), new RefractiveMaterial(3f, new SolidColourTexture(White), true)),
					},
					new Light[]
					{
							new SimpleLight
							{
									Position                = new Vector3(555/2f,540,555/2f),
									Colour                  = White,
									AttenuationRadius                  = 350f,
									DistanceAttenuationFunc=SimpleLight.ExponentialDecayDistanceAttenuation(1f)
									// DistanceAttenuationFunc=SimpleLight.LogisticsCurveDistanceAttenuation()
							}
					},
					new SingleColourSkyBox(Black)
			);
		}
	}

	/// <summary>Fancy scene containing (hopefully) every type of shape, light and material</summary>
	public static Scene Demo
	{
		get
		{
			List<SceneObject> objects = new();
			List<Light>       lights  = new();
			Camera            camera  = Camera.Create(new Vector3(0, 2.87f, 7), UnitY * 3, UnitY, 70, 16f / 9f, 0f, 1f);

			//Ground plane
			{
				objects.Add(new SceneObject("Ground", new InfinitePlane(new Vector3(0, -0.001f, 0), UnitY), new StandardMaterial(new MarbleTexture(), new SolidColourTexture(Black), .5f)));
			}
			{
				//Demonstrates axis-aligned planes and semi-diffuse materials
				Vector3 low = new(-7, 0, -2), high = new(-5, 2.8f, -.5f);
				objects.Add(new SceneObject("XY", new XYPlane(low.X, high.X, low.Y, high.Y, low.Z), new StandardMaterial(new Colour(1f,  .5f, .5f), .5f)));
				objects.Add(new SceneObject("YZ", new YZPlane(low.Y, high.Y, low.Z, high.Z, low.X), new StandardMaterial(new Colour(.5f, 1f,  .5f), .5f)));
				objects.Add(new SceneObject("XZ", new XZPlane(low.X, high.X, low.Z, high.Z, low.Y), new StandardMaterial(new Colour(.5f, .5f, 1f),  Black, .5f)));

				//Demonstrates emission from material
				objects.Add(new SceneObject("Planes Sphere Light", new Sphere(((low + high) / 2f) - UnitY, .5f), new StandardMaterial(Black, White * 0.8f, 0f)));
			}
			{
				//Demonstrates reflective materials
				objects.Add(new SceneObject("Lonely Sphere", new Sphere(new Vector3(-1, 3f, -2), 1f), new StandardMaterial(new Colour(165 / 255f, 42 / 255f, 42 / 255f), 0f)));
				//Demonstrates refractive materials
				objects.Add(new SceneObject("Capsule", new Capsule(new Vector3(-2, .7f, -3), new Vector3(0, 1.5f, -1f), .7f), new RefractiveMaterial(RefractiveMaterial.GlassIndex, new SolidColourTexture(new Colour(0.27058825F, 0.77254903F, 1F)))));
			}

			{
				//Infinite point light
				lights.Add(new InfinitePointLight(new Vector3(-1, 5, -2), Red * .25f));
				//Position this slightly above the light so it doesn't affect the shadow calculations
				objects.Add(new SceneObject("Infinite Light Visualiser", new Sphere(new Vector3(-1, 5.1f, -2), .05f), new StandardMaterial(Black, Red, 0f)));

				//Same but with a sized (area) light
				lights.Add(new PointLight(new Vector3(-5, 1f, -7f), Green, 1.5f));
				objects.Add(new SceneObject("Sized Light Visualiser", new Sphere(new Vector3(-5, 1.1f, -7f), .05f), new StandardMaterial(Black, Green, 0f)));
				objects.Add(new SceneObject("Sized Light Blocker",    new Sphere(new Vector3(-5, .6f,  -7f), .2f),  new StandardMaterial(Black, 0f)));

				//Same but the light is diffuse
				lights.Add(new DiffuseSphereLight(new Vector3(3, 1f, -7f), .3f, Blue, 2f));
				objects.Add(new SceneObject("Diffuse Light Visualiser", new Sphere(new Vector3(3, 1.1f, -7f), .1f), new StandardMaterial(Black, Blue, 0f)));
				objects.Add(new SceneObject("Diffuse Light Blocker",    new Sphere(new Vector3(3, .6f,  -7f), .3f), new StandardMaterial(Black, 0f)));
			}

			{
				//Cuboids
				objects.Add(new SceneObject("Smoke Box", new ConstantDensityMedium(Box.CreateFromCorners(new Vector3(-4, 0, 0), new Vector3(-1, 1, 2)), 2f), new VolumetricMaterial(Black)));
				objects.Add(new SceneObject("Hex Box",   Box.CreateFromCorners(new Vector3(-3, 0.75f, 0.5f), new Vector3(-2, 1.25f, 1.5f)),                  new StandardMaterial(Orange * .5f, 1f)));
			}

			{
				//Bounded planar objects
				objects.Add(new SceneObject("Disk", new Disk(new Vector3(5, .5f, 1), Normalize(new Vector3(-1, 1, -1)), .7f),      new StandardMaterial(Purple * .6f, .3f)));
				objects.Add(new SceneObject("Quad", new Quad(new Vector3(2, 0,   1), new Vector3(0, 1, -1), new Vector3(1, 0, 0)), new StandardMaterial(Yellow * .6f, .1f)));
			}

			return new Scene("Demo", camera, objects.ToArray(), lights.ToArray(), new DefaultSkyBox());
		}
	}

	/// <summary>Simple scene with a single sphere at (0, 0, 0)</summary>
	public static Scene Sphere => new(
			"A lonely Sphere", Camera.Create(new Vector3(0, 0, 2), new Vector3(0.1f, 0f, 0f), UnitY, 20, 16f / 9f, 0, 1f), new SceneObject[]
			{
					new("Sphere", new Sphere(Zero, .1f), new StandardMaterial(Red, 1f))
			},
			Array.Empty<Light>(),
			new DefaultSkyBox()
	);

	/// <summary>Simple scene with two spheres at (+-1, 0, 0)</summary>
	public static Scene RgbSpheres => new(
			"RGB Spheres", Camera.Create(new Vector3(0, 0, 5), Zero, UnitY, 5, 16f / 9f, .00002f, 5f), new SceneObject[]
			{
					new("Sphere 1", new Sphere(new Vector3(0.1f),  .1f), new StandardMaterial(Lerp(Red,   White, 0.5f), 1f)),
					new("Sphere 2", new Sphere(new Vector3(0),     .1f), new StandardMaterial(Lerp(Green, White, 0.5f), 1f)),
					new("Sphere 3", new Sphere(new Vector3(-0.1f), .1f), new StandardMaterial(Lerp(Blue,  White, 0.5f), 1f))
					// new("Plane", new InfinitePlane(Zero, UnitZ), new RefractiveMaterial(1, White * .5f, Black))
			},
			Array.Empty<Light>(),
			new DefaultSkyBox()
	);

	/// <summary>The good 'ol cornell box, as is traditional for raytracing</summary>
	public static Scene CornellBox
	{
		get
		{
			Material greyWallMaterial = new StandardMaterial(new Colour(0.73f, 0.73f, 0.73f), 1f);
			return new Scene(
					"Cornell Box", Camera.Create(new Vector3(278, 278, -800), new Vector3(278, 278, 0), UnitY, 40f, 1f / 1f, 0f, 1f), new SceneObject[]
					{
							new("Left", new YZPlane(0,  555, 0, 555, 0), new StandardMaterial(new Colour(0.5f,   0.1f, 0.1f), 1f)),
							new("Right", new YZPlane(0, 555, 0, 555, 555), new StandardMaterial(new Colour(0.1f, 0.5f, 0.1f), 1f)),
							new("Back", new XYPlane(0, 555, 0, 555, 555), greyWallMaterial),
							new("Top", new XZPlane(0,    555, 0, 555, 555), greyWallMaterial),
							new("Bottom", new XZPlane(0, 555, 0, 555, 0), greyWallMaterial),

							new("Light", new XZPlane(213, 343, 227, 332, 554.9f), new StandardMaterial(White, White, 1f)),

							new("Small Box", new Box(Matrix4x4.CreateScale(165, 165, 165) * Matrix4x4.CreateFromYawPitchRoll(-18 * (PI / 180f), 0 * (PI / 180f), 0 * (PI / 180f)) * Matrix4x4.CreateTranslation(212.5f, 82.5f, 147.5f)), new StandardMaterial(new Colour(0.73f, 0.73f, 0.73f), 1f)),
							new("Tall Box", new Box(Matrix4x4.CreateScale(165,  330, 165) * Matrix4x4.CreateFromYawPitchRoll(15  * (PI / 180f), 0 * (PI / 180f), 0 * (PI / 180f)) * Matrix4x4.CreateTranslation(347.5f, 165f,  377.5f)), new StandardMaterial(new Colour(0.73f, 0.73f, 0.73f), 1f)),
							new("Small Box Sphere", new Sphere(new Vector3(212.5f, 265f, 147.5f), 100), new EmissiveRefractiveMaterial(RefractiveMaterial.GlassIndex, new SolidColourTexture(White), new SolidColourTexture(Blue * 0.1f), true)),
							new("Tall Box Sphere", new Sphere(new Vector3(347.5f,  430f, 377.5f), 100), new RefractiveMaterial(RefractiveMaterial.GlassIndex, new SolidColourTexture(White)))
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


	/// <summary>Cover for RayTracing in a weekend, chapter one</summary>
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
							sphereMaterial = new StandardMaterial(albedo, 1f);
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
							sphereMaterial = new StandardMaterial(albedo, 1 - fuzz);
						}
						else if (chooseMat < 0.655)
						{
							Colour             colour = RandomColour(HalfGrey, White);
							SurfaceSphereLight light  = new(center, 0.4f, colour, 1f);
							lights.Add(light);
							sphereMaterial = new StandardMaterial(White, 0f);
						}
						else
						{
							// glass
							Colour tint = RandomColour(Black, White);
							sphereMaterial = new RefractiveMaterial(RandomFloat(1f, 5f), new SolidColourTexture(tint));
						}

						objects.Add(new SceneObject($"Sphere ({a},{b})", new Sphere(center, 0.2f), sphereMaterial));
					}
				}
			}

			objects.Add(new SceneObject("Sphere A", new Sphere(new Vector3(0,  1, 0), 1), new RefractiveMaterial(1.5f, new SolidColourTexture(White))));
			objects.Add(new SceneObject("Sphere B", new Sphere(new Vector3(-4, 1, 0), 1), new StandardMaterial(new Colour(.4f, .2f, .1f), 1f)));
			objects.Add(new SceneObject("Sphere C", new Sphere(new Vector3(4,  1, 0), 1), new StandardMaterial(new Colour(.7f, .6f, .5f), 0f)));
			objects.Add(new SceneObject("Ground",   new InfinitePlane(Zero, UnitY),       new StandardMaterial(new Colour(0.5f),          Black, 1f)));
			return new Scene("RayTracing Chapter 1", Camera.Create(new Vector3(13, 2, 3), Zero, UnitY, 20, 16f / 9f, 0f, 10f), objects.ToArray(), lights.ToArray(), new DefaultSkyBox());
		}
	}

	/// <summary>Gets all the builtin scenes</summary>
	public static IEnumerable<Scene> GetAll()
	{
		return typeof(BuiltinScenes)
				.GetProperties(BindingFlags.Public | BindingFlags.Static)
				.Where(p => p.PropertyType == typeof(Scene))
				.Select(p => (Scene)p.GetValue(null)!);
	}
}