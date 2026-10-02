using BenchmarkDotNet.Running;
using ServiceStack.OrmLite.Tests.Benchmarks;

// Select benchmarks with a filter, e.g: dotnet run -c Release -- --filter *CompiledQuery*
if (args.Length > 0)
    BenchmarkSwitcher.FromAssembly(typeof(LargeColumnBenchmark).Assembly).Run(args);
else
    BenchmarkRunner.Run<LargeColumnBenchmark>();
