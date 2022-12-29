using System.IO;
using NUnit.Framework;
using ServiceStack.Text.Tests.DynamicModels;

namespace ServiceStack.Text.Tests.Benchmarks
{
    public class JsonDeserializationBenchmarks
    {
        static ModelWithAllTypes allTypesModel = ModelWithAllTypes.Create(3);
        static ModelWithCommonTypes commonTypesModel = ModelWithCommonTypes.Create(3);
        static MemoryStream stream = new MemoryStream(32768);
        const string serializedString = "this is the test string";
        readonly string serializedString256 = new string('t', 256);
        readonly string serializedString512 = new string('t', 512);
        readonly string serializedString4096 = new string('t', 4096);

        static string commonTypesModelJson;
        static string stringTypeJson;

        static JsonDeserializationBenchmarks()
        {
            commonTypesModelJson = JsonSerializer.SerializeToString<ModelWithCommonTypes>(commonTypesModel);
            stringTypeJson = JsonSerializer.SerializeToString<StringType>(StringType.Create());
        }

        [Test]
        public void DeserializeJsonCommonTypes()
        {
            var result = JsonSerializer.DeserializeFromString<ModelWithCommonTypes>(commonTypesModelJson);
        }

        [Test]
        public void DeserializeStringType()
        {
            var result = JsonSerializer.DeserializeFromString<StringType>(stringTypeJson);
        }
    }
}