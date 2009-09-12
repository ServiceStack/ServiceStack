using System;
using System.Runtime.Serialization;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Common.Extensions;
using ServiceStack.Logging;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Results;
using ServiceStack.WebHost.Endpoints.Tests.Mocks;
using DataContractSerializer = ServiceStack.ServiceModel.Serialization.DataContractSerializer;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	[DataContract(Namespace = "http://schemas.ddnglobal.com/types/")]
	public class TestCompress
	{
		[DataMember]
		public int Id { get; set; }

		[DataMember]
		public string Name { get; set; }

		public TestCompress()
		{
		}

		public TestCompress(int id, string name)
		{
			Id = id;
			Name = name;
		}
	}


	[TestFixture]
	public class CompressionTests
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof (CompressionTests));

		[Test]
		public void Can_compress_and_decompress_SimpleDto()
		{
			var simpleDto = new TestCompress(1, "name");

			var simpleDtoXml = DataContractSerializer.Instance.Parse(simpleDto);

			var simpleDtoZip = simpleDtoXml.Compress();

			Assert.That(simpleDtoZip.Length, Is.GreaterThan(0));

			var deserializedSimpleDtoXml = simpleDtoZip.Decompress();

			Assert.That(deserializedSimpleDtoXml, Is.Not.Empty);

			var deserializedSimpleDto = DataContractDeserializer.Instance.Parse<TestCompress>(
				deserializedSimpleDtoXml);

			Assert.That(deserializedSimpleDto, Is.Not.Null);

			Assert.That(deserializedSimpleDto.Id, Is.EqualTo(simpleDto.Id));
			Assert.That(deserializedSimpleDto.Name, Is.EqualTo(simpleDto.Name));
		}


		[Test]
		public void Test_response_with_CompressedResult()
		{
			var mockResponse = new HttpResponseMock();

			var simpleDto = new TestCompress(1, "name");

			var simpleDtoXml = DataContractSerializer.Instance.Parse(simpleDto);

			const string expectedXml = "<TestCompress xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://schemas.ddnglobal.com/types/\"><Id>1</Id><Name>name</Name></TestCompress>";

			Assert.That(simpleDtoXml, Is.EqualTo(expectedXml));

			var simpleDtoZip = simpleDtoXml.Compress();

			var compressedResult = new CompressedResult(simpleDtoZip);

			var reponseWasAutoHandled = mockResponse.WriteToResponse(
				compressedResult, CompressionTypes.Deflate);

			Assert.That(reponseWasAutoHandled, Is.True);

			var writtenBytes = mockResponse.GetOutputStreamAsBytes();
			Assert.That(writtenBytes, Is.EqualTo(simpleDtoZip));
			Assert.That(mockResponse.Headers[HttpHeaders.ContentType], Is.EqualTo(MimeTypes.Xml));
			Assert.That(mockResponse.Headers[HttpHeaders.ContentEncoding], Is.EqualTo(CompressionTypes.Deflate));

			Log.Debug("Content-length: " + writtenBytes.Length);
			Log.Debug(BitConverter.ToString(writtenBytes));
		}



		[Test]
		public void Can_gzip_and_gunzip_SimpleDto()
		{
			var simpleDto = new TestCompress(1, "name");

			var simpleDtoXml = DataContractSerializer.Instance.Parse(simpleDto);

			var simpleDtoZip = simpleDtoXml.Gzip();

			Assert.That(simpleDtoZip.Length, Is.GreaterThan(0));

			var deserializedSimpleDtoXml = simpleDtoZip.Gunzip();

			Assert.That(deserializedSimpleDtoXml, Is.Not.Empty);

			var deserializedSimpleDto = DataContractDeserializer.Instance.Parse<TestCompress>(
				deserializedSimpleDtoXml);

			Assert.That(deserializedSimpleDto, Is.Not.Null);

			Assert.That(deserializedSimpleDto.Id, Is.EqualTo(simpleDto.Id));
			Assert.That(deserializedSimpleDto.Name, Is.EqualTo(simpleDto.Name));
		}

	}
}