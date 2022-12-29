``` ini

BenchmarkDotNet=v0.10.14, OS=Windows 10.0.17134
Intel Core i7-7700K CPU 4.20GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=2.1.301
  [Host]     : .NET Core 2.1.1 (CoreCLR 4.6.26606.02, CoreFX 4.6.26606.05), 64bit RyuJIT  [AttachedDebugger]
  DefaultJob : .NET Core 2.1.1 (CoreCLR 4.6.26606.02, CoreFX 4.6.26606.05), 64bit RyuJIT


```
|            Method |     N |     Mean |     Error |    StdDev |   Median |
|------------------ |------ |---------:|----------:|----------:|---------:|
| NetCoreParseInt32 | 10000 | 58.28 ns | 0.5349 ns | 0.5004 ns | 58.19 ns |
| NetCoreParseInt34 | 10000 | 59.95 ns | 1.2276 ns | 2.6425 ns | 58.93 ns |
|  CustomParseInt32 | 10000 | 12.11 ns | 0.2584 ns | 0.2417 ns | 12.05 ns |
|  CustomParseInt64 | 10000 | 11.28 ns | 0.0723 ns | 0.0564 ns | 11.27 ns |
