``` ini

BenchmarkDotNet=v0.10.14, OS=Windows 10.0.17134
Intel Core i7-7700K CPU 4.20GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=2.1.301
  [Host]     : .NET Core 2.1.1 (CoreCLR 4.6.26606.02, CoreFX 4.6.26606.05), 64bit RyuJIT
  DefaultJob : .NET Core 2.1.1 (CoreCLR 4.6.26606.02, CoreFX 4.6.26606.05), 64bit RyuJIT


```
|                       Method |     N |     Mean |     Error |    StdDev |    Gen 0 |    Gen 1 |    Gen 2 | Allocated |
|----------------------------- |------ |---------:|----------:|----------:|---------:|---------:|---------:|----------:|
|        DeserializeFromStream | 10000 | 7.069 ms | 0.0331 ms | 0.0277 ms | 226.5625 | 109.3750 |  39.0625 |   1.25 MB |
| DeserializeFromStreamJsonNet | 10000 | 8.432 ms | 0.1628 ms | 0.2059 ms | 218.7500 | 109.3750 |  31.2500 |   1.25 MB |
|            SerializeToString | 10000 | 2.649 ms | 0.0200 ms | 0.0167 ms | 304.6875 | 156.2500 | 156.2500 |   1.21 MB |
|     SerializeToStringJsonNet | 10000 | 4.632 ms | 0.0664 ms | 0.0621 ms | 304.6875 | 257.8125 | 156.2500 |   1.45 MB |
