using BenchmarkDotNet.Attributes;
using RayTracer.Core;
using System.Numerics;
using static System.Numerics.Vector3;

// ReSharper disable all


namespace RayTracer.Benchmarks;

[SimpleJob]
public class Benchmarks
{
	public Ray     ray;
	public Vector3 normal;
	public Vector3 point;
	public float   negPointDotNormal;
	public float   invNormDotDir;

	public Benchmarks()
	{
		ray               = new Ray(RandUtils.RandomInUnitCube() *1000f, RandUtils.RandomOnUnitSphere());
		normal            = RandUtils.RandomOnUnitSphere();
		point             = RandUtils.RandomInUnitCube() * 1000f;
		negPointDotNormal = -Dot(point, normal);
	}


	[Benchmark]
	public float Method1()
	{
		return -Dot(normal, ray.Origin - point) /Dot(normal, ray.Direction) ;
	}

	[Benchmark]
	public float Method2()
	{
		return  -(Dot(ray.Origin, normal) -Dot(point, normal)) / Dot(normal, ray.Direction);
	}

	[Benchmark]
	public float Method2Cached()
	{
		return  -(Dot(ray.Origin, normal) + negPointDotNormal) / Dot(normal, ray.Direction);
	}
}