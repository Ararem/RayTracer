using BenchmarkDotNet.Attributes;
using RayTracer.Core;
using RayTracer.Impl;
using RayTracer.Impl.Hittables;
using RayTracer.Impl.Lights;
using RayTracer.Impl.Skyboxes;
using System.Numerics;
using static System.Numerics.Vector3;

// ReSharper disable all


namespace RayTracer.Benchmarks;

[SimpleJob]
public class Benchmarks
{
	public DiffuseShapedLight ShapedLight;
	public DiffuseSphereLight SphereLight;
	public HitRecord          hit;

	public Benchmarks()
	{
		Vector3 centre = RandUtils.RandomInUnitCube() * 1000;
		float   radius = RandUtils.RandomFloat01()    * 500;
		Sphere  sphere = new Sphere(centre, radius);
		ShapedLight = new DiffuseShapedLight(sphere);
		SphereLight = new DiffuseSphereLight() { Position = centre, DiffusionRadius = radius };
		hit         = new HitRecord(new Ray(Zero, UnitX), Zero, Zero, UnitY, 0f, true, Vector2.One, null!, null);
		AsyncRenderJob renderJob = new AsyncRenderJob(new Scene("Test", Camera.Create(Zero, Zero, UnitY, 90, 1, 1, 1), new SceneObject[]{new SceneObject("", sphere)}, new Light[]{SphereLight, ShapedLight}, new DefaultSkyBox()), RenderOptions.Default);
	}

	[Benchmark]
	public Colour Shaped()
	{
		return ShapedLight.CalculateLight(hit);
	}

	[Benchmark]
	public Colour Sphere()
	{
		return SphereLight.CalculateLight(hit);
	}
}