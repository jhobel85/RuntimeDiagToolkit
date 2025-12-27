```

BenchmarkDotNet v0.13.10, Windows 11 (10.0.22631.6345/23H2/2023Update/SunValley3)
11th Gen Intel Core i7-11850H 2.50GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 9.0.307
  [Host]   : .NET 8.0.22 (8.0.2225.52707), X64 RyuJIT AVX2
  ShortRun : .NET 8.0.22 (8.0.2225.52707), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=5  LaunchCount=1  
WarmupCount=1  

```
| Method          | Mean            | Error            | StdDev         |
|---------------- |----------------:|-----------------:|---------------:|
| CpuUsage        |    30,299.06 ns |     9,714.792 ns |   1,503.375 ns |
| MemorySnapshot  | 4,049,088.98 ns | 1,823,960.370 ns | 473,676.716 ns |
| GcStats         |       111.32 ns |        30.145 ns |       7.829 ns |
| ThreadPoolStats |        55.80 ns |         9.224 ns |       2.395 ns |
