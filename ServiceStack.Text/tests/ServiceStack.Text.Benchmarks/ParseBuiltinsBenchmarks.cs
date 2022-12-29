using System;
using System.Globalization;
using BenchmarkDotNet.Attributes;
using ServiceStack.Text;

namespace ServiceStack.Text.Benchmarks
{
    public class ParseBuiltinBenchmarks
    {
        const string int32_1 = "1234";
        const string int32_2 = "-1234";
        const string decimal_1 = "1234.5678";
        const string decimal_2 = "-1234.5678";
        const string decimal_3 = "1234.5678901234567890";
        const string decimal_4 = "-1234.5678901234567890";
        const string guid_1 = "{b6170a18-3dd7-4a9b-b5d6-21033b5ad162}";

        [Benchmark]
        public void Int32Parse()
        {
            var res1 = JsonSerializer.DeserializeFromString<int>(int32_1);
            var res2 = JsonSerializer.DeserializeFromString<int>(int32_2);
        }

        [Benchmark]
        public void DecimalParse()
        {
            var res1 = JsonSerializer.DeserializeFromString<decimal>(decimal_1);
            var res2 = JsonSerializer.DeserializeFromString<decimal>(decimal_2);
        }

        [Benchmark]
        public void BigDecimalParse()
        {
            var res1 = JsonSerializer.DeserializeFromString<decimal>(decimal_3);
            var res2 = JsonSerializer.DeserializeFromString<decimal>(decimal_4);
        }

        [Benchmark]
        public void GuidParse()
        {
            var res1 = JsonSerializer.DeserializeFromString<Guid>(guid_1);
        }
    }
}