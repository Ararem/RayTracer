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
	public float a = RandUtils.RandomFloat01(), b = RandUtils.RandomFloat01(), t = RandUtils.RandomFloat01();
	public Benchmarks()
	{
	}

	[Benchmark]
	public float SumBilinear()
	{
		return ((1f - t) * a) + (b * t);
	}

	[Benchmark]
	public float DiffLinear()
	{
		return a + (t * (b-a));
	}
}