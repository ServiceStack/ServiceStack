using NUnit.Framework;
using ServiceStack.CacheAccess.Providers.Tests.Models;
using ServiceStack.Common.Extensions;
using ServiceStack.Compression;
using ServiceStack.ServiceModel.Serialization;

namespace ServiceStack.CacheAccess.Providers.Tests
{
	[TestFixture]
	public class CompressionICSharpProviderTests
	{
		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			StreamExtensions.DeflateProvider = new ICSharpDeflateProvider();
			StreamExtensions.GZipProvider = new ICSharpGZipProvider();
		}

		[Test]
		public void ICSharp_can_deflate_and_inflate()
		{
			const int sizeWhenDoesntWork = 6;

			var dto = ModelWithFieldsOfDifferentTypes.Create(1);

			var dtoXml = DataContractSerializer.Instance.Parse(dto);

			var zipXml = dtoXml.Deflate();

			Assert.That(zipXml.Length, Is.GreaterThan(sizeWhenDoesntWork));

			var unzipXml = zipXml.Inflate();

			Assert.That(unzipXml, Is.EqualTo(dtoXml));
		}

		[Test]
		public void ICSharp_can_gzip_and_gunzip()
		{
			const int sizeWhenDoesntWork = 6;

			var dto = ModelWithFieldsOfDifferentTypes.Create(1);

			var dtoXml = DataContractSerializer.Instance.Parse(dto);

			var zipXml = dtoXml.GZip();

			Assert.That(zipXml.Length, Is.GreaterThan(sizeWhenDoesntWork));

			var unzipXml = zipXml.GUnzip();

			Assert.That(unzipXml, Is.EqualTo(dtoXml));
		}

	}
}