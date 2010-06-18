using System;
using NUnit.Framework;
using ServiceStack.CacheAccess.Providers.Tests.Models;
using ServiceStack.Common.Utils;
using ServiceStack.ServiceModel.Serialization;

namespace ServiceStack.CacheAccess.Providers.Tests
{
	[TestFixture]
	public class XmlCacheManagerTests
	{
		public ICacheClient CreateCacheClient()
		{
			//return new MemcachedClientCache(AppConfig.MemcachedServers);
			return new MemoryCacheClient();
		}

		[Test]
		public void Caches_results_as_string()
		{
			var dto = ModelWithFieldsOfDifferentTypes.Create(1);
			var expectedXml = DataContractSerializer.Instance.Parse(dto);

			var cacheManager = new XmlCacheManager(CreateCacheClient());

			var cacheKey = dto.CreateUrn();
			var dtoXml = cacheManager.ResolveText(cacheKey, () => dto);

			Assert.That(dtoXml, Is.EqualTo(expectedXml));
		}

		[Test]
		public void Subsequent_requests_get_from_cache()
		{
			var i = 0;

			var cacheFn = (Func<ModelWithFieldsOfDifferentTypes>)
			(() => ModelWithFieldsOfDifferentTypes.Create(i++));

			var dto = cacheFn();

			var cacheManager = new XmlCacheManager(CreateCacheClient());

			var cacheKey = dto.CreateUrn();
			var dtoXml = cacheManager.ResolveText(cacheKey, cacheFn);

			var dto1 = DataContractDeserializer.Instance
				.Parse<ModelWithFieldsOfDifferentTypes>(dtoXml);

			Assert.That(dto1.Id, Is.EqualTo(1));

			dtoXml = cacheManager.ResolveText(cacheKey, cacheFn);

			var dto2 = DataContractDeserializer.Instance
				.Parse<ModelWithFieldsOfDifferentTypes>(dtoXml);

			Assert.That(dto2.Id, Is.EqualTo(1));
		}
	}

}