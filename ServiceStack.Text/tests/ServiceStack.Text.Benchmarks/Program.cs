using System;
using System.Linq;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Columns;
using BenchmarkDotNet.Attributes.Exporters;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace ServiceStack.Text.Benchmarks
{
    public class Md5VsSha256
    {
        private const int N = 10000;
        private readonly byte[] data;

        private readonly SHA256 sha256 = SHA256.Create();
        private readonly MD5 md5 = MD5.Create();

        public Md5VsSha256()
        {
            data = new byte[N];
            new Random(42).NextBytes(data);
        }

        [Benchmark]
        public byte[] Sha256() => sha256.ComputeHash(data);

        [Benchmark]
        public byte[] Md5() => md5.ComputeHash(data);
    }
    
    public class AllowNonOptimized : ManualConfig
    {
        public AllowNonOptimized()
        {
            Add(JitOptimizationsValidator.DontFailOnError); // ALLOW NON-OPTIMIZED DLLS

            Add(DefaultConfig.Instance.GetLoggers().ToArray()); // manual config has no loggers by default
            Add(DefaultConfig.Instance.GetExporters().ToArray()); // manual config has no exporters by default
            Add(DefaultConfig.Instance.GetColumnProviders().ToArray()); // manual config has no columns by default
        }
    }
    
    public class Program
    {
        public static void Main(string[] args)
        {
#if true
            Summary summary;
//            summary = BenchmarkRunner.Run<Md5VsSha256>();
//            summary = BenchmarkRunner.Run<MemoryProviderBenchmarks>();
//            summary = BenchmarkRunner.Run<MemoryDecimalBenchmarks>();
//            summary = BenchmarkRunner.Run<MemoryIntegerBenchmarks>();
            summary = BenchmarkRunner.Run<BookShelf10000BooksBenchmarks>();
#else
            var test = new BookShelf10000BooksBenchmarks();
            test.Setup();
            for (var i = 0; i < 200; i++)
            {
//                test.DeserializeFromString();
                test.SerializeToString();
            }
#endif
        }
    }    
}
