using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceModel.Serialization;

namespace ServiceStack.CacheAccess.Providers.Tests
{
	[TestFixture]
	public class ContentCacheManagerTests
	{
		const string CacheKey = "urn:cachedresponses:homepage";
		ICacheClient cacheClient;
		ModelWithIdAndName model;
		string xmlModel;
		private IRequestContext serializationContext;

		[SetUp]
		public void OnBeforeEachTest()
		{
			cacheClient = new MemoryCacheClient();
			//cacheClient = new RedisCacheClient(TestConfig.SingleHost);
			cacheClient.FlushAll();
			model = ModelWithIdAndName.Create(1);
			xmlModel = DataContractSerializer.Instance.Parse(model);
			serializationContext = new SerializationContext(MimeTypes.Xml);
		}

		[Test]
		public void Resolve_with_no_CompressionType_does_not_cache_compressed_result()
		{
			var xmlResult = ContentCacheManager.Resolve(
				() => model,
				serializationContext,
				cacheClient,
				CacheKey,
				null);

			Assert.That(xmlResult, Is.EqualTo(xmlModel));

			var cachedResult = cacheClient.Get<ModelWithIdAndName>(CacheKey);
			ModelWithIdAndName.AssertIsEqual(model, cachedResult);

			var serializedCacheKey = ContentCacheManager.GetSerializedCacheKey(CacheKey, MimeTypes.Xml);
			var serializedCachedResult = cacheClient.Get<string>(serializedCacheKey);
			Assert.That(serializedCachedResult, Is.EqualTo(xmlModel));

			AssertNoCompressedResults(cacheClient, serializedCacheKey);
		}

		[Test]
		public void Resolve_twice_with_no_CompressionType_does_not_cache_compressed_result()
		{
			var xmlResult = ContentCacheManager.Resolve(
				() => model,
				serializationContext,
				cacheClient,
				CacheKey,
				null);

			Assert.That(xmlResult, Is.EqualTo(xmlModel));

			xmlResult = ContentCacheManager.Resolve(
				() => model,
				serializationContext,
				cacheClient,
				CacheKey,
				null);

			Assert.That(xmlResult, Is.EqualTo(xmlModel));

			var cachedResult = cacheClient.Get<ModelWithIdAndName>(CacheKey);
			ModelWithIdAndName.AssertIsEqual(model, cachedResult);

			var serializedCacheKey = ContentCacheManager.GetSerializedCacheKey(CacheKey, MimeTypes.Xml);
			var serializedCachedResult = cacheClient.Get<string>(serializedCacheKey);
			Assert.That(serializedCachedResult, Is.EqualTo(xmlModel));

			AssertNoCompressedResults(cacheClient, serializedCacheKey);
		}

		[Test]
		public void Clear_after_Resolve_with_MimeType_clears_all_cached_results()
		{
			ContentCacheManager.Resolve(
				() => model,
				serializationContext,
				cacheClient,
				CacheKey,
				null);

			ContentCacheManager.Clear(cacheClient, CacheKey);

			AssertEmptyCache(cacheClient, CacheKey);
		}

		[Test]
		public void Clear_after_Resolve_with_MimeType_and_CompressionType_clears_all_cached_results()
		{
			var serializationContext = new SerializationContext(MimeTypes.Xml)
			{
				CompressionType = CompressionTypes.Deflate
			};
			ContentCacheManager.Resolve(
				() => model,
				serializationContext,
				cacheClient,
				CacheKey,
				null);

			ContentCacheManager.Clear(cacheClient, CacheKey);

			AssertEmptyCache(cacheClient, CacheKey);
		}

		[Test]
		public void Can_cache_null_result()
		{
			var xmlResult = ContentCacheManager.Resolve<ModelWithIdAndName>(
				() => null,
				serializationContext,
				cacheClient,
				CacheKey,
				null);

			Assert.That(xmlResult, Is.Null);

			var cachedResult = cacheClient.Get<ModelWithIdAndName>(CacheKey);
			Assert.That(cachedResult, Is.Null);

			var serializedCacheKey = ContentCacheManager.GetSerializedCacheKey(CacheKey, MimeTypes.Xml);
			var serializedCachedResult = cacheClient.Get<string>(serializedCacheKey);
			Assert.That(serializedCachedResult, Is.Null);

			AssertNoCompressedResults(cacheClient, serializedCacheKey);
		}


		private static void AssertEmptyCache(
			ICacheClient cacheClient, string cacheKey)
		{
			AssertNoCachedResult(cacheClient, cacheKey);
			AssertNoSerializedResults(cacheClient, cacheKey);
			AssertNoCompressedResults(cacheClient, cacheKey);
		}

		private static void AssertNoCachedResult(
			ICacheClient cacheClient, string cacheKey)
		{
			var cachedResult = cacheClient.Get<ModelWithIdAndName>(cacheKey);
			Assert.That(cachedResult, Is.Null);
		}

		private static void AssertNoSerializedResults(
			ICacheClient cacheClient, string cacheKey)
		{
			foreach (var mimeType in ContentCacheManager.AllCachedContentTypes)
			{
				var serializedCacheKey = ContentCacheManager.GetSerializedCacheKey(
					cacheKey, mimeType);

				var serializedResult = cacheClient.Get<string>(serializedCacheKey);
				Assert.That(serializedResult, Is.Null);
			}
		}

		private static void AssertNoCompressedResults(
			ICacheClient cacheClient, string serializedCacheKey)
		{
			foreach (var compressionType in CompressionTypes.AllCompressionTypes)
			{
				var compressedCacheKey = ContentCacheManager.GetCompressedCacheKey(
					serializedCacheKey, compressionType);

				var compressedCachedResult = cacheClient.Get<byte[]>(compressedCacheKey);
				Assert.That(compressedCachedResult, Is.Null);
			}
		}

	}
}