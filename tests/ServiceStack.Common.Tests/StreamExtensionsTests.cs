using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using NUnit.Framework;
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Web;
using ServiceStack.ServiceModel.Serialization;
using DataContractSerializer=ServiceStack.ServiceModel.Serialization.DataContractSerializer;

namespace ServiceStack.Common.Tests
{
	[TestFixture]
	public class StreamExtensionsTests
	{
		[DataContract]
		public class SimpleDto
		{
			[DataMember]
			public int Id { get; set; }

			[DataMember]
			public string Name { get; set; }

			public SimpleDto(int id, string name)
			{
				Id = id;
				Name = name;
			}
		}

		[Test]
		public void Can_compress_and_decompress_SimpleDto()
		{
			var simpleDto = new SimpleDto(1, "name");

			var simpleDtoXml = DataContractSerializer.Instance.Parse(simpleDto);

			var simpleDtoZip = simpleDtoXml.Deflate();

			Assert.That(simpleDtoZip.Length, Is.GreaterThan(0));

			var deserializedSimpleDtoXml = simpleDtoZip.Inflate();

			Assert.That(deserializedSimpleDtoXml, Is.Not.Empty);

			var deserializedSimpleDto = DataContractDeserializer.Instance.Parse<SimpleDto>(
				deserializedSimpleDtoXml);

			Assert.That(deserializedSimpleDto, Is.Not.Null);

			Assert.That(deserializedSimpleDto.Id, Is.EqualTo(simpleDto.Id));
			Assert.That(deserializedSimpleDto.Name, Is.EqualTo(simpleDto.Name));
		}

		[Test]
		public void Can_compress_and_decompress_SimpleDto_with_Gzip()
		{
			var simpleDto = new SimpleDto(1, "name");

			var simpleDtoXml = DataContractSerializer.Instance.Parse(simpleDto);

			var simpleDtoZip = simpleDtoXml.GZip();

			Assert.That(simpleDtoZip.Length, Is.GreaterThan(0));

			var deserializedSimpleDtoXml = simpleDtoZip.GUnzip();

			Assert.That(deserializedSimpleDtoXml, Is.Not.Empty);

			var deserializedSimpleDto = DataContractDeserializer.Instance.Parse<SimpleDto>(
				deserializedSimpleDtoXml);

			Assert.That(deserializedSimpleDto, Is.Not.Null);

			Assert.That(deserializedSimpleDto.Id, Is.EqualTo(simpleDto.Id));
			Assert.That(deserializedSimpleDto.Name, Is.EqualTo(simpleDto.Name));
		}

        [Test]
        public void WritePartialTo_has_reasonable_timeout_for_invalid_requests()
        {
            var contentStream = new MemoryStream(Encoding.UTF8.GetBytes("0123456789"));
            var outputStream = new MemoryStream();
            var writeTimer = Stopwatch.StartNew();
            try
            {
                contentStream.WritePartialTo(outputStream, 0, 100);
            }
            catch
            {
            }
            writeTimer.Stop();
            Assert.That(writeTimer.ElapsedMilliseconds < 1000);
        }
	}
}