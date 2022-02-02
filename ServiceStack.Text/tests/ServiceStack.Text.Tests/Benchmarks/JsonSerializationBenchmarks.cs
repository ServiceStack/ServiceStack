using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using ServiceStack.Text.Json;
using ServiceStack.Text.Tests.DynamicModels;

namespace ServiceStack.Text.Tests.Benchmarks
{
    public class JsonSerializationBenchmarks
    {
        static ModelWithAllTypes allTypesModel = ModelWithAllTypes.Create(3);
        static ModelWithCommonTypes commonTypesModel = ModelWithCommonTypes.Create(3);
        static MemoryStream stream = new MemoryStream(32768);
        const string serializedString = "this is the test string";
        readonly string serializedString256 = new string('t', 256);
        readonly string serializedString512 = new string('t', 512);
        readonly string serializedString4096 = new string('t', 4096);

        [Test]
        public void SerializeJsonAllTypes()
        {
            string result = JsonSerializer.SerializeToString<ModelWithAllTypes>(allTypesModel);
        }

        [Test]
        public void SerializeJsonCommonTypes()
        {
            string result = JsonSerializer.SerializeToString<ModelWithCommonTypes>(commonTypesModel);
        }

        [Test]
        public void SerializeJsonString()
        {
            string result = JsonSerializer.SerializeToString<string>(serializedString);
        }

        [Test]
        public void SerializeJsonStringToStream()
        {
            stream.Position = 0;
            JsonSerializer.SerializeToStream<string>(serializedString, stream);
        }

        [Test]
        public void SerializeJsonString256ToStream()
        {
            stream.Position = 0;
            JsonSerializer.SerializeToStream<string>(serializedString256, stream);
        }

        [Test]
        public void SerializeJsonString512ToStream()
        {
            stream.Position = 0;
            JsonSerializer.SerializeToStream<string>(serializedString512, stream);
        }

        [Test]
        public void SerializeJsonString4096ToStream()
        {
            stream.Position = 0;
            JsonSerializer.SerializeToStream<string>(serializedString4096, stream);
        }

        [Test]
        public void SerializeJsonStringToStreamDirectly()
        {
            stream.Position = 0;
            string tmp = JsonSerializer.SerializeToString<string>(serializedString);
            byte[] arr = Encoding.UTF8.GetBytes(tmp);
            stream.Write(arr, 0, arr.Length);
        }


        [Test]
        public void SerializeJsonAllTypesToStream()
        {
            stream.Position = 0;
            JsonSerializer.SerializeToStream<ModelWithAllTypes>(allTypesModel, stream);
        }

        [Test]
        public void SerializeJsonCommonTypesToStream()
        {
            stream.Position = 0;
            JsonSerializer.SerializeToStream<ModelWithCommonTypes>(commonTypesModel, stream);
        }

        [Test]
        public void SerializeJsonStringToStreamUsingDirectStreamWriter()
        {
            stream.Position = 0;
            var writer = new DirectStreamWriter(stream, JsConfig.UTF8Encoding);
            JsonWriter<string>.WriteRootObject(writer, serializedString);
            writer.Flush();
        }

        [Test]
        public void SerializeJsonString256ToStreamUsingDirectStreamWriter()
        {
            stream.Position = 0;
            var writer = new DirectStreamWriter(stream, JsConfig.UTF8Encoding);
            JsonWriter<string>.WriteRootObject(writer, serializedString256);
            writer.Flush();
        }

        [Test]
        public void SerializeJsonString512ToStreamUsingDirectStreamWriter()
        {
            stream.Position = 0;
            var writer = new DirectStreamWriter(stream, JsConfig.UTF8Encoding);
            JsonWriter<string>.WriteRootObject(writer, serializedString512);
            writer.Flush();
        }

        [Test]
        public void SerializeJsonString4096ToStreamUsingDirectStreamWriter()
        {
            stream.Position = 0;
            var writer = new DirectStreamWriter(stream, JsConfig.UTF8Encoding);
            JsonWriter<string>.WriteRootObject(writer, serializedString4096);
            writer.Flush();
        }
    }
}