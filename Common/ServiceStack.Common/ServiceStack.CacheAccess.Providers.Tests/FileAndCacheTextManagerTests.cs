using System;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.CacheAccess.Providers.Tests.Models;
using ServiceStack.Common.Utils;
using ServiceStack.Common.Web;

namespace ServiceStack.CacheAccess.Providers.Tests
{
	[TestFixture]
	public class FileAndCacheTextManagerTests
	{
		[Test]
		public void It_works()
		{
			var cacheManager = new FileAndCacheTextManager
				(@"~\App_Data\".MapAbsolutePath(), new XmlCacheManager(new MemoryCacheClient()));

			var id = Guid.NewGuid();

			var cacheKey = IdUtils.CreateCacheKeyPath<ModelWithFieldsOfDifferentTypes>(id.ToString());

			var cacheFn = (Func<ModelWithFieldsOfDifferentTypes>)
			              (() => ModelWithFieldsOfDifferentTypes.Create(1));

			var result = cacheManager.Resolve(CompressionTypes.Deflate, cacheKey, cacheFn);

			Assert.That(result, Is.Not.Null);
		}
	}
}