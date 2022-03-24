using BenchmarkDotNet.Attributes;
using JetBrains.Annotations;

//ReSharper disable all
namespace RayTracer.Benchmarks;

[SimpleJob]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class PropertyFieldBenchmarks
{
	private const    int     Iterations = 1_000_000;
	private readonly object? readonlyField;
	private          object? writeableField;
	private          object? readonlyProperty  { get; }
	private          object? writeableProperty { get; set; }

	[Benchmark]
	public void WriteableField()
	{
		for (int i = 0; i < Iterations; i++) GC.KeepAlive(writeableField);
	}

	[Benchmark]
	public void ReadonlyField()
	{
		for (int i = 0; i < Iterations; i++) GC.KeepAlive(readonlyField);
	}

	[Benchmark]
	public void ReadonlyProperty()
	{
		for (int i = 0; i < Iterations; i++) GC.KeepAlive(readonlyProperty);
	}

	[Benchmark]
	public void WriteableProperty()
	{
		for (int i = 0; i < Iterations; i++) GC.KeepAlive(writeableProperty);
	}
}