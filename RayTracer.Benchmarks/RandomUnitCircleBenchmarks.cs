using BenchmarkDotNet.Attributes;
using JetBrains.Annotations;
using RayTracer.Core;
using System.Numerics;

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
	public Vector2 RandRadiusTheta()
	{
		float theta = Rand.RandomFloat(0, 2 * MathF.PI);
		float r     = MathF.Sqrt(Rand.RandomFloat());
		(float x, float y) = MathF.SinCos(theta);
		return new Vector2(r * x, r * y);
	}
}