using Ararem.RayTracer.Core;
using Ararem.RayTracer.Impl.Hittables;
using Ararem.RayTracer.Impl.Lights;
using Ararem.RayTracer.Impl.Materials;
using Ararem.RayTracer.Impl.Skyboxes;
using Ararem.RayTracer.Impl.Textures;
using JetBrains.Annotations;
using System.Numerics;
using System.Reflection;
using static Ararem.RayTracer.Core.Colour;
using static Ararem.RayTracer.Core.RandUtils;
using static System.Numerics.Vector3;
using static System.MathF;

namespace Ararem.RayTracer.Impl.Builtin;

/// <summary>Static class that contains a list of pre-made scenes</summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static class BuiltinScenes
{
	/// <summary>Testing scene</summary>
	public static Scene Testing
	{
		get
		{
			return new Scene(
					"Testing", Camera.Create(new Vector3(0, 0, -5), new Vector3(0), UnitY, 90, 16f / 9f, 0f, 1f), new SceneObject[]
					{
							new("Disk", new Disk(UnitX, Normalize(new Vector3(1, 1, 1)), 3f) { Material = new StandardMaterial(HalfGrey, 0f) })
					},
					Array.Empty<Light>(),
					new DefaultSkyBox()
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
				objects.Add(new SceneObject("Ground", new InfinitePlane(new Vector3(0, -0.001f, 0), UnitY) { Material = new StandardMaterial(new MarbleTexture(), new SolidColourTexture(Black), .5f) }));
			}
			{
				//Demonstrates axis-aligned planes and semi-diffuse materials
				Vector3 low = new(-7, 0, -2), high = new(-5, 2.8f, -.5f);
				objects.Add(new SceneObject("XY", new XYPlane(low.X, high.X, low.Y, high.Y, low.Z) { Material = new StandardMaterial(new Colour(1f,  .5f, .5f), .5f) }));
				objects.Add(new SceneObject("YZ", new YZPlane(low.Y, high.Y, low.Z, high.Z, low.X) { Material = new StandardMaterial(new Colour(.5f, 1f,  .5f), .5f) }));
				objects.Add(new SceneObject("XZ", new XZPlane(low.X, high.X, low.Z, high.Z, low.Y) { Material = new StandardMaterial(new Colour(.5f, .5f, 1f),  Black, .5f) }));

				//Demonstrates emission from material
				objects.Add(new SceneObject("Planes Sphere Light", new Sphere(((low + high) / 2f) - UnitY, .5f) { Material = new StandardMaterial(Black, White * 0.8f, 0f) }));
			}
			{
				//Demonstrates reflective materials
				objects.Add(new SceneObject("Lonely Sphere", new Sphere(new Vector3(-1, 3f, -2), 1f) { Material = new StandardMaterial(new Colour(165 / 255f, 42 / 255f, 42 / 255f), 0f) }));
				//Demonstrates refractive materials
				objects.Add(new SceneObject("Capsule", new Capsule(new Vector3(-2, .7f, -3), new Vector3(0, 1.5f, -1f), .7f) { Material = new RefractiveMaterial(RefractiveMaterial.GlassIndex, new SolidColourTexture(new Colour(0.27058825F, 0.77254903F, 1F))) }));
			}

			{
				//Infinite point light
				//By changing the radii and attenuation function, we make this an 'infinite' light that has the same brightness regardless of distance
				lights.Add(new PointLight { Position = new Vector3(-1, 5, -2), Colour = Red * .25f, AttenuationRadius = float.PositiveInfinity, CutoffRadius = float.PositiveInfinity, DistanceAttenuationFunc = static (_, _) => 1f });
				//Position this slightly above the light so it doesn't affect the shadow calculations
				objects.Add(new SceneObject("Infinite Light Visualiser", new Sphere(new Vector3(-1, 5.1f, -2), .05f) { Material = new StandardMaterial(Black, Red, 0f) }));

				//Same but with a sized (area) light
				lights.Add(new PointLight { Position = new Vector3(-5, 1f, -7f), Colour = Green, AttenuationRadius = 1.5f });
				objects.Add(new SceneObject("Sized Light Visualiser", new Sphere(new Vector3(-5, 1.1f, -7f), .05f) { Material = new StandardMaterial(Black, Green, 0f) }));
				objects.Add(new SceneObject("Sized Light Blocker",    new Sphere(new Vector3(-5, .6f,  -7f), .2f) { Material  = new StandardMaterial(Black, 0f) }));

				//Same but the light is diffuse
				lights.Add(
						new DiffuseSphereLight
						{
								Position = new Vector3(3, 1f, -7f), DiffusionRadius = .3f, Colour = Blue, AttenuationRadius = 2f
						}
				);
				objects.Add(new SceneObject("Diffuse Light Visualiser", new Sphere(new Vector3(3, 1.1f, -7f), .1f) { Material = new StandardMaterial(Black, Blue, 0f) }));
				objects.Add(new SceneObject("Diffuse Light Blocker",    new Sphere(new Vector3(3, .6f,  -7f), .3f) { Material = new StandardMaterial(Black, 0f) }));
			}

			{
				//Cuboids
				objects.Add(new SceneObject("Smoke Box", new ConstantDensityMedium(new Box(new Vector3(-4, 0, 0), new Vector3(-1, 1, 2)), 2f, Black)));
				objects.Add(new SceneObject("Hex Box",   new Box(new Vector3(-3, 0.75f, 0.5f), new Vector3(-2, 1.25f, 1.5f)) { Material = new StandardMaterial(Orange * .5f, 1f) }));
			}

			{
				//Bounded planar objects
				objects.Add(new SceneObject("Disk", new Disk(new Vector3(5, .5f, 1), Normalize(new Vector3(-1, 1, -1)), .7f) { Material      = new StandardMaterial(Purple * .6f, .3f) }));
				objects.Add(new SceneObject("Quad", new Quad(new Vector3(2, 0,   1), new Vector3(0, 1, -1), new Vector3(1, 0, 0)) { Material = new StandardMaterial(Yellow * .6f, .1f) }));
			}

			return new Scene("Demo", camera, objects.ToArray(), lights.ToArray(), new DefaultSkyBox());
		}
	}

	/// <summary>Simple scene with a single sphere at (0, 0, 0)</summary>
	public static Scene Sphere => new(
			"A lonely Sphere", Camera.Create(new Vector3(0, 0, 2), new Vector3(0.1f, 0f, 0f), UnitY, 20, 16f / 9f, 0, 1f), new SceneObject[]
			{
					new("Sphere", new Sphere(Zero, .1f) { Material = new StandardMaterial(Red, 1f) })
			},
			Array.Empty<Light>(),
			new DefaultSkyBox()
	);

	/// <summary>Simple scene with two spheres at (+-1, 0, 0)</summary>
	public static Scene RgbSpheres => new(
			"RGB Spheres", Camera.Create(new Vector3(0, 0, 5), Zero, UnitY, 5, 16f / 9f, .00002f, 5f), new SceneObject[]
			{
					new("Sphere 1", new Sphere(new Vector3(0.1f),  .1f) { Material = new StandardMaterial(Lerp(Red,   White, 0.5f), 1f) }),
					new("Sphere 2", new Sphere(new Vector3(0),     .1f) { Material = new StandardMaterial(Lerp(Green, White, 0.5f), 1f) }),
					new("Sphere 3", new Sphere(new Vector3(-0.1f), .1f) { Material = new StandardMaterial(Lerp(Blue,  White, 0.5f), 1f) })
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
							new("Left", new YZPlane(0,  555, 0, 555, 0) { Material    = new StandardMaterial(new Colour(0.5f, 0.1f, 0.1f), 1f) }),
							new("Right", new YZPlane(0, 555, 0, 555, 555) { Material  = new StandardMaterial(new Colour(0.1f, 0.5f, 0.1f), 1f) }),
							new("Back", new XYPlane(0, 555, 0, 555, 555) { Material   = greyWallMaterial }),
							new("Top", new XZPlane(0,    555, 0, 555, 555) { Material = greyWallMaterial }),
							new("Bottom", new XZPlane(0, 555, 0, 555, 0) { Material   = greyWallMaterial }),

							new("Light", new XZPlane(213, 343, 227, 332, 554.9f) { Material = new StandardMaterial(White, White, 1f) }),

							new("Small Box", new Box(Matrix4x4.CreateScale(165, 165, 165) * Matrix4x4.CreateFromYawPitchRoll(-18 * (PI / 180f), 0 * (PI / 180f), 0 * (PI / 180f)) * Matrix4x4.CreateTranslation(212.5f, 82.5f, 147.5f)) { Material = new StandardMaterial(new Colour(0.73f, 0.73f, 0.73f), 1f) }),
							new("Tall Box", new Box(Matrix4x4.CreateScale(165,  330, 165) * Matrix4x4.CreateFromYawPitchRoll(15  * (PI / 180f), 0 * (PI / 180f), 0 * (PI / 180f)) * Matrix4x4.CreateTranslation(347.5f, 165f,  377.5f)) { Material = new StandardMaterial(new Colour(0.73f, 0.73f, 0.73f), 1f) }),
							new("Small Box Sphere", new Sphere(new Vector3(212.5f, 265f, 147.5f), 100) { Material                                                                                                                                  = new EmissiveRefractiveMaterial(RefractiveMaterial.GlassIndex, new SolidColourTexture(White), new SolidColourTexture(Blue * 0.1f), true) }),
							new("Tall Box Sphere", new Sphere(new Vector3(347.5f,  430f, 377.5f), 100) { Material                                                                                                                                  = new RefractiveMaterial(RefractiveMaterial.GlassIndex, new SolidColourTexture(White)) })
					},
					new Light[]
					{
							//Centre ceiling light
							new DiffuseSphereLight { Position = new Vector3((213 + 343) / 2f, 554 - 50, (227 + 332) / 2f), DiffusionRadius = 40, Colour = White * 0.5f, AttenuationRadius = 150 }
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
							DiffuseSphereLight light  = new() { Position = center, Colour = colour, AttenuationRadius = 1f, DiffusionRadius = .4f };
							lights.Add(light);
							sphereMaterial = new StandardMaterial(White, 0f);
						}
						else
						{
							// glass
							Colour tint = RandomColour(Black, White);
							sphereMaterial = new RefractiveMaterial(RandomFloat(1f, 5f), new SolidColourTexture(tint));
						}

						objects.Add(new SceneObject($"Sphere ({a},{b})", new Sphere(center, 0.2f) { Material = sphereMaterial }));
					}
				}
			}

			objects.Add(new SceneObject("Sphere A", new Sphere(new Vector3(0,  1, 0), 1) { Material = new RefractiveMaterial(1.5f, new SolidColourTexture(White)) }));
			objects.Add(new SceneObject("Sphere B", new Sphere(new Vector3(-4, 1, 0), 1) { Material = new StandardMaterial(new Colour(.4f, .2f, .1f), 1f) }));
			objects.Add(new SceneObject("Sphere C", new Sphere(new Vector3(4,  1, 0), 1) { Material = new StandardMaterial(new Colour(.7f, .6f, .5f), 0f) }));
			objects.Add(new SceneObject("Ground",   new InfinitePlane(Zero, UnitY) { Material       = new StandardMaterial(new Colour(0.5f),          Black, 1f) }));
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