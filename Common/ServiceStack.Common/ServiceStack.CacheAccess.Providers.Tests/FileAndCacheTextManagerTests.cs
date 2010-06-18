using System;
using System.IO;
using NUnit.Framework;
using ServiceStack.CacheAccess.Providers.Tests.Models;
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Utils;
using ServiceStack.Common.Web;
using ServiceStack.Compression;
using ServiceStack.ServiceModel.Serialization;

namespace ServiceStack.CacheAccess.Providers.Tests
{
	[TestFixture]
	public class FileAndCacheTextManagerTests
	{
		private ICacheClient cacheClient;
		private XmlCacheManager cacheTextManager;
		private FileAndCacheTextManager fileCacheManager;
		readonly string appDataPath = @"~\App_Data\".MapAbsolutePath();
		private string idValue;
		private CacheKeyTuple cacheKeys;

		[SetUp]
		public void SetUp()
		{
			//StreamExtensionsTests.DeflateProvider = new ICSharpDeflateProvider();
			//StreamExtensionsTests.GZipProvider = new ICSharpGZipProvider();

			this.cacheClient = new MemoryCacheClient();
			this.cacheTextManager = new XmlCacheManager(this.cacheClient);
			this.fileCacheManager = Create(cacheTextManager);
			this.idValue = Guid.NewGuid().ToString();
			this.cacheKeys = GetCacheKeys<ModelWithFieldsOfDifferentTypes>(idValue);

			if (Directory.Exists(appDataPath))
			{
				try
				{
					Directory.Delete(appDataPath, true);
					Directory.CreateDirectory(appDataPath);
				}
				catch (Exception ignore) {} 
			}
		}

		public FileAndCacheTextManager Create(XmlCacheManager xmlCacheManager)
		{
			return new FileAndCacheTextManager(appDataPath, xmlCacheManager);
		}

		public class CacheKeyTuple
		{
			public string CacheKey { get; set; }
			public string ContentTypeCacheKey { get; set; }
			public string CompressionTypeCacheKey { get; set; }
		}

		public CacheKeyTuple GetCacheKeys<T>(string id)
		{
			var cacheKey = IdUtils.CreateCacheKeyPath<T>(id);

			var contentType = cacheKey + MimeTypes.GetExtension(MimeTypes.Xml);
			var compressionType = contentType + CompressionTypes.GetExtension(CompressionTypes.Deflate);

			return new CacheKeyTuple {
				CacheKey = cacheKey,
				ContentTypeCacheKey = contentType,
				CompressionTypeCacheKey = compressionType,
			};
		}

		[Test]
		public void FileCacheManager_creates_memory_and_file_caches()
		{
			var dto = ModelWithFieldsOfDifferentTypes.Create(1);

			var cacheFn = (Func<ModelWithFieldsOfDifferentTypes>) (() => dto);

			var result = fileCacheManager.Resolve(CompressionTypes.Deflate, cacheKeys.CacheKey, cacheFn);

			var compressedResult = result as CompressedResult;

			Assert.That(compressedResult, Is.Not.Null);

			var dtoXml = DataContractSerializer.Instance.Parse(dto);
			var xmlResultInMemory = this.cacheTextManager.CacheClient.Get<string>(cacheKeys.ContentTypeCacheKey);
			Assert.That(xmlResultInMemory, Is.EqualTo(dtoXml));

			var dtoZip = dtoXml.Deflate();
			var zipXmlResultInMemory = this.cacheTextManager.CacheClient.Get<byte[]>(cacheKeys.CompressionTypeCacheKey);
			Assert.That(dtoZip, Is.EquivalentTo(zipXmlResultInMemory));

			var xmlFilePath = Path.Combine(appDataPath, cacheKeys.ContentTypeCacheKey);
			var zipXmlFilePath = Path.Combine(appDataPath, cacheKeys.CompressionTypeCacheKey);

			Assert.That(File.Exists(xmlFilePath), Is.True);
			Assert.That(File.Exists(zipXmlFilePath), Is.True);
		}

		[Test]
		public void FileCacheManager_clears_memory_and_caches()
		{
			FileCacheManager_creates_memory_and_file_caches();

			fileCacheManager.Clear(cacheKeys.CacheKey);

			var xmlResultInMemory = this.cacheTextManager.CacheClient.Get<string>(cacheKeys.ContentTypeCacheKey);
			Assert.That(xmlResultInMemory, Is.Null);

			var xmlFilePath = Path.Combine(appDataPath, cacheKeys.ContentTypeCacheKey);
			var zipXmlFilePath = Path.Combine(appDataPath, cacheKeys.CompressionTypeCacheKey);

			Assert.That(File.Exists(xmlFilePath), Is.False);
			Assert.That(File.Exists(zipXmlFilePath), Is.False);
		}

		[Test]
		public void Caches_in_file_and_memory_are_the_same()
		{
			FileCacheManager_creates_memory_and_file_caches();

			var xmlResultInMemory = this.cacheTextManager.CacheClient.Get<string>(
				cacheKeys.ContentTypeCacheKey);

			var zipXmlResultInMemory = this.cacheTextManager.CacheClient.Get<byte[]>(
				cacheKeys.CompressionTypeCacheKey);

			var xmlFilePath = Path.Combine(appDataPath, cacheKeys.ContentTypeCacheKey);
			var zipXmlFilePath = Path.Combine(appDataPath, cacheKeys.CompressionTypeCacheKey);

			var xmlResultInFile = File.ReadAllText(xmlFilePath);
			var zipXmlResultInFile = File.ReadAllBytes(zipXmlFilePath);

			Assert.That(xmlResultInMemory, Is.EqualTo(xmlResultInFile));
			Assert.That(zipXmlResultInMemory, Is.EqualTo(zipXmlResultInFile));
		}

		[Test]
		public void No_CompressionType_only_caches_xml()
		{
			var dto = ModelWithFieldsOfDifferentTypes.Create(1);

			var cacheFn = (Func<ModelWithFieldsOfDifferentTypes>)(() => dto);

			var result = fileCacheManager.Resolve(null, cacheKeys.CacheKey, cacheFn);

			var xmlResult = result as string;

			Assert.That(xmlResult, Is.Not.Null);

			var dtoXml = DataContractSerializer.Instance.Parse(dto);
			var xmlResultInMemory = this.cacheTextManager.CacheClient.Get<string>(cacheKeys.ContentTypeCacheKey);
			Assert.That(xmlResultInMemory, Is.EqualTo(dtoXml));

			var zipXmlResultInMemory = this.cacheTextManager.CacheClient.Get<byte[]>(cacheKeys.CompressionTypeCacheKey);
			Assert.That(zipXmlResultInMemory, Is.Null);

			var xmlFilePath = Path.Combine(appDataPath, cacheKeys.ContentTypeCacheKey);
			var zipXmlFilePath = Path.Combine(appDataPath, cacheKeys.CompressionTypeCacheKey);

			Assert.That(File.Exists(xmlFilePath), Is.True);
			Assert.That(File.Exists(zipXmlFilePath), Is.False);
		}

	}
}