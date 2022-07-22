``` ini

BenchmarkDotNet=v0.13.1, OS=pop 22.04
Intel Core i7-7700HQ CPU 2.80GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
.NET SDK=6.0.300
  [Host]     : .NET 6.0.5 (6.0.522.21309), X64 RyuJIT
  DefaultJob : .NET 6.0.5 (6.0.522.21309), X64 RyuJIT
  Job-SUISCW : .NET 6.0.5 (6.0.522.21309), X64 RyuJIT


```

|      Method |        Job | Toolchain |      Mean |     Error |    StdDev |
|------------ |----------- |---------- |----------:|----------:|----------:|
| SumBilinear | DefaultJob |   Default | 0.5853 ns | 0.0536 ns | 0.0896 ns |
|  DiffLinear | DefaultJob |   Default | 0.0427 ns | 0.0400 ns | 0.0375 ns |
| SumBilinear | Job-SUISCW |  .NET 6.0 | 0.2636 ns | 0.0452 ns | 0.0768 ns |
|  DiffLinear | Job-SUISCW |  .NET 6.0 | 0.0427 ns | 0.0076 ns | 0.0063 ns |
