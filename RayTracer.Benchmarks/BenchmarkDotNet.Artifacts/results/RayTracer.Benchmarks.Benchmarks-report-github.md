``` ini

BenchmarkDotNet=v0.13.1, OS=pop 22.04
Intel Core i7-7700HQ CPU 2.80GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
.NET SDK=6.0.300
  [Host]     : .NET 6.0.5 (6.0.522.21309), X64 RyuJIT
  DefaultJob : .NET 6.0.5 (6.0.522.21309), X64 RyuJIT
  Job-DNLUEN : .NET 6.0.5 (6.0.522.21309), X64 RyuJIT


```

|        Method |        Job | Toolchain |     Mean |     Error |    StdDev |
|-------------- |----------- |---------- |---------:|----------:|----------:|
|       Method1 | DefaultJob |   Default | 2.131 ns | 0.0774 ns | 0.1134 ns |
|       Method2 | DefaultJob |   Default | 2.709 ns | 0.0951 ns | 0.1423 ns |
| Method2Cached | DefaultJob |   Default | 1.890 ns | 0.0809 ns | 0.1134 ns |
|       Method1 | Job-DNLUEN |  .NET 6.0 | 2.184 ns | 0.0887 ns | 0.1382 ns |
|       Method2 | Job-DNLUEN |  .NET 6.0 | 2.671 ns | 0.0866 ns | 0.1126 ns |
| Method2Cached | Job-DNLUEN |  .NET 6.0 | 2.001 ns | 0.0825 ns | 0.1333 ns |
