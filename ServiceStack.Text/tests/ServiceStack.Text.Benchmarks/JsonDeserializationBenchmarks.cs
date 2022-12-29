using System;
using System.IO;
using System.Text;
using BenchmarkDotNet.Attributes;
using ServiceStack.Text;
using ServiceStack.Text.Json;
using StackExchange.Profiling;

namespace ServiceStack.Text.Benchmarks
{
    public class StringType
    {
        public string Value1 { get; set; }
        public string Value2 { get; set; }
        public string Value3 { get; set; }
        public string Value4 { get; set; }
        public string Value5 { get; set; }
        public string Value6 { get; set; }
        public string Value7 { get; set; }

        public static StringType Create()
        {
            var st = new StringType();
 	    st.Value1 = st.Value2 = st.Value3 = st.Value4 = st.Value5 = st.Value6 = st.Value7 = "Hello, world";
            return st;
        }
    } 

    [MemoryDiagnoser]
    public class JsonDeserializationBenchmarks
    {
        static ModelWithCommonTypes commonTypesModel = ModelWithCommonTypes.Create(3);
        static MemoryStream stream = new MemoryStream(32768);
        const string serializedString = "this is the test string";
        readonly string serializedString256 = new string('t', 256);
        readonly string serializedString512 = new string('t', 512);
        readonly string serializedString4096 = new string('t', 4096);
        
        static string commonTypesModelJson;
        static ReadOnlyMemory<char> commonTypesModelJsonSpan;

        static string stringTypeJson; 
        static ReadOnlyMemory<char> stringTypeJsonSpan; 

        static JsonDeserializationBenchmarks()
        {
            commonTypesModelJson = JsonSerializer.SerializeToString<ModelWithCommonTypes>(commonTypesModel);
            commonTypesModelJsonSpan = commonTypesModelJson.AsMemory(); 

            stringTypeJson = JsonSerializer.SerializeToString<StringType>(StringType.Create());
            stringTypeJsonSpan = stringTypeJson.AsMemory();
        }

        [Benchmark(Description = "Deserialize Json: class with builtin types (string)")]
        public void DeserializeJsonCommonTypesString()
        {
            var result = JsonSerializer.DeserializeFromString<ModelWithCommonTypes>(commonTypesModelJson);
        }

        [Benchmark(Description = "Deserialize Json: class with builtin types (span)")]
        public void DeserializeJsonCommonTypesSpan()
        {
            var result = JsonSerializer.DeserializeFromString<ModelWithCommonTypes>(commonTypesModelJson);
        }

        [Benchmark(Description = "Deserialize Json: class with 10 string properties")]
        public void DeserializeStringType()
        {
            var result = JsonSerializer.DeserializeFromString<StringType>(stringTypeJson);
        }

        [Benchmark(Description = "Deserialize Json: Complex MiniProfiler")]
        public MiniProfiler ComplexDeserializeServiceStack() => ServiceStack.Text.JsonSerializer.DeserializeFromString<MiniProfiler>(_complexProfilerJson);

        private static readonly MiniProfiler _complexProfiler = GetComplexProfiler();
        private static readonly string _complexProfilerJson = _complexProfiler.ToJson();

        private static MiniProfiler GetComplexProfiler()
        {
            var mp = new MiniProfiler("Complex");
            for (var i = 0; i < 50; i++)
            {
                using (mp.Step("Step " + i))
                {
                    for (var j = 0; j < 50; j++)
                    {
                        using (mp.Step("SubStep " + j))
                        {
                            for (var k = 0; k < 50; k++)
                            {
                                using (mp.CustomTiming("Custom " + k, "YOLO!"))
                                {
                                }
                            }
                        }
                    }
                }
            }
            return mp;
        }
    }
}
