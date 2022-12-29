``` ini

BenchmarkDotNet=v0.10.14, OS=Windows 10.0.17134
Intel Core i7-7700K CPU 4.20GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=2.1.301
  [Host]     : .NET Core 2.1.1 (CoreCLR 4.6.26606.02, CoreFX 4.6.26606.05), 64bit RyuJIT  [AttachedDebugger]
  DefaultJob : .NET Core 2.1.1 (CoreCLR 4.6.26606.02, CoreFX 4.6.26606.05), 64bit RyuJIT


```
|              Method |     N |      Mean |     Error |    StdDev |
|-------------------- |------ |----------:|----------:|----------:|
| NetCoreParseDecimal | 10000 | 192.23 ns | 3.8236 ns | 4.8357 ns |
|  CustomParseDecimal | 10000 |  45.22 ns | 0.4465 ns | 0.3728 ns |
