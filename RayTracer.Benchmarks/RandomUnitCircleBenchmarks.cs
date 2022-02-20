using BenchmarkDotNet.Attributes;
using JetBrains.Annotations;
using RayTracer.Core;
using System.Numerics;
using static System.MathF;

namespace RayTracer.Benchmarks;

[SimpleJob]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class RandomUnitCircleBenchmarks
{
	[Benchmark]
	public Vector2 RandXYLoop()
	{
		while (true)
		{
			Vector2 p = new(Rand.RandomPlusMinusOne(), Rand.RandomPlusMinusOne());
			if (p.LengthSquared() >= 1) continue;
			return p;
		}
	}

	[Benchmark]
	public Vector2 RandRadiusThetaTuple()
	{
		float theta = Rand.RandomFloat(0, 2 * PI);
		float r     = Sqrt(Rand.RandomFloat());
		(float x, float y) = SinCos(theta);
		return new Vector2(r * x, r * y);
	}

	[Benchmark]
	public Vector2 RandRadiusThetaPreMult()
	{
		float theta = Rand.RandomFloat(0, 2 * PI);
		float r     = Sqrt(Rand.RandomFloat());
		return new Vector2(r * Cos(theta), r * Sin(theta));
	}

	[Benchmark]
	public Vector2 RandRadiusThetaPostMult()
	{
		float theta = Rand.RandomFloat(0, 2 * PI);
		float r     = Sqrt(Rand.RandomFloat());
		return r * new Vector2(Cos(theta), Sin(theta));
	}
}