``` ini

BenchmarkDotNet=v0.10.14, OS=Windows 10.0.17134
Intel Core i7-7700K CPU 4.20GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=2.1.301
  [Host]     : .NET Core 2.1.1 (CoreCLR 4.6.26606.02, CoreFX 4.6.26606.05), 64bit RyuJIT  [AttachedDebugger]
  DefaultJob : .NET Core 2.1.1 (CoreCLR 4.6.26606.02, CoreFX 4.6.26606.05), 64bit RyuJIT


```
|              Method |      N |      Mean |     Error |    StdDev |
|-------------------- |------- |----------:|----------:|----------:|
| **DefaultParseBoolean** |   **1000** |  **30.00 ns** | **0.6018 ns** | **0.5911 ns** |
| NetCoreParseBoolean |   1000 |  20.09 ns | 0.1783 ns | 0.1489 ns |
|  DefaulParseDecimal |   1000 |  54.57 ns | 1.3897 ns | 1.7067 ns |
| NetCoreParseDecimal |   1000 | 189.92 ns | 3.3742 ns | 3.3139 ns |
|    DefaulParseFloat |   1000 | 117.37 ns | 0.9250 ns | 0.8652 ns |
|   NetCoreParseFloat |   1000 | 102.20 ns | 0.6069 ns | 0.5677 ns |
|   DefaulParseDouble |   1000 | 118.04 ns | 2.3821 ns | 2.3395 ns |
|  NetCoreParseDouble |   1000 | 101.87 ns | 1.1663 ns | 0.9739 ns |
|    DefaulParseInt32 |   1000 |  19.23 ns | 0.0790 ns | 0.0700 ns |
|   NetCoreParseInt32 |   1000 |  72.63 ns | 1.4722 ns | 1.8080 ns |
|    DefaulParseInt64 |   1000 |  18.35 ns | 0.1577 ns | 0.1231 ns |
|   NetCoreParseInt64 |   1000 |  69.79 ns | 0.5492 ns | 0.5138 ns |
|   DefaulParseUInt32 |   1000 |  18.66 ns | 0.1509 ns | 0.1260 ns |
|  NetCoreParseUInt32 |   1000 |  72.68 ns | 0.3446 ns | 0.3054 ns |
| **DefaultParseBoolean** |  **10000** |  **28.35 ns** | **0.1571 ns** | **0.1470 ns** |
| NetCoreParseBoolean |  10000 |  19.39 ns | 0.0590 ns | 0.0552 ns |
|  DefaulParseDecimal |  10000 |  57.40 ns | 1.1499 ns | 1.8239 ns |
| NetCoreParseDecimal |  10000 | 192.12 ns | 2.7811 ns | 2.6014 ns |
|    DefaulParseFloat |  10000 | 126.15 ns | 2.5386 ns | 4.2414 ns |
|   NetCoreParseFloat |  10000 | 106.11 ns | 1.6362 ns | 1.5305 ns |
|   DefaulParseDouble |  10000 | 122.10 ns | 1.9895 ns | 1.8610 ns |
|  NetCoreParseDouble |  10000 | 107.38 ns | 2.0829 ns | 1.9483 ns |
|    DefaulParseInt32 |  10000 |  19.97 ns | 0.3522 ns | 0.3122 ns |
|   NetCoreParseInt32 |  10000 |  72.41 ns | 0.8461 ns | 0.7500 ns |
|    DefaulParseInt64 |  10000 |  18.19 ns | 0.0536 ns | 0.0502 ns |
|   NetCoreParseInt64 |  10000 |  69.90 ns | 0.8358 ns | 0.6979 ns |
|   DefaulParseUInt32 |  10000 |  18.58 ns | 0.1056 ns | 0.0936 ns |
|  NetCoreParseUInt32 |  10000 |  72.05 ns | 0.3941 ns | 0.3291 ns |
| **DefaultParseBoolean** | **100000** |  **28.40 ns** | **0.1632 ns** | **0.1447 ns** |
| NetCoreParseBoolean | 100000 |  19.38 ns | 0.1022 ns | 0.0853 ns |
|  DefaulParseDecimal | 100000 |  51.66 ns | 0.2337 ns | 0.2071 ns |
| NetCoreParseDecimal | 100000 | 185.71 ns | 1.0153 ns | 0.9000 ns |
|    DefaulParseFloat | 100000 | 119.73 ns | 2.4091 ns | 3.0467 ns |
|   NetCoreParseFloat | 100000 | 103.48 ns | 0.9406 ns | 0.8798 ns |
|   DefaulParseDouble | 100000 | 117.82 ns | 1.0134 ns | 0.9480 ns |
|  NetCoreParseDouble | 100000 | 102.38 ns | 0.3972 ns | 0.3316 ns |
|    DefaulParseInt32 | 100000 |  19.41 ns | 0.2591 ns | 0.2424 ns |
|   NetCoreParseInt32 | 100000 |  72.14 ns | 0.7835 ns | 0.7329 ns |
|    DefaulParseInt64 | 100000 |  18.33 ns | 0.1323 ns | 0.1237 ns |
|   NetCoreParseInt64 | 100000 |  70.20 ns | 0.5148 ns | 0.4815 ns |
|   DefaulParseUInt32 | 100000 |  19.11 ns | 0.4070 ns | 0.4180 ns |
|  NetCoreParseUInt32 | 100000 |  74.99 ns | 1.3515 ns | 1.1980 ns |
