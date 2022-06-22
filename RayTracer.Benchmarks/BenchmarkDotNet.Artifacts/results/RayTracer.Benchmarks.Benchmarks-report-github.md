``` ini

BenchmarkDotNet=v0.13.1, OS=pop 22.04
Intel Core i7-7700HQ CPU 2.80GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
.NET SDK=6.0.300
  [Host]     : .NET 6.0.5 (6.0.522.21309), X64 RyuJIT
  DefaultJob : .NET 6.0.5 (6.0.522.21309), X64 RyuJIT
  Job-RCERVS : .NET 6.0.5 (6.0.522.21309), X64 RyuJIT


```
| Method |        Job | Toolchain |     Mean |    Error |   StdDev |
|------- |----------- |---------- |---------:|---------:|---------:|
| Shaped | DefaultJob |   Default | 459.0 ns | 19.13 ns | 56.42 ns |
| Sphere | DefaultJob |   Default | 259.4 ns |  4.33 ns | 11.01 ns |
| Shaped | Job-RCERVS |  .NET 6.0 | 437.4 ns | 15.87 ns | 46.79 ns |
| Sphere | Job-RCERVS |  .NET 6.0 | 316.8 ns |  8.79 ns | 25.91 ns |
