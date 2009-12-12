using Moq;
using NUnit.Framework;
using ServiceStack.CacheAccess.Providers.Tests.Models;
using ServiceStack.Common.Utils;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Sqlite;

namespace ServiceStack.CacheAccess.Providers.Tests
{
	[TestFixture]
	public class OrmLitePersistenceProviderCacheTests
	{
		private const string ConnectionString = ":memory:";

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			OrmLite.OrmLiteConfig.DialectProvider = new SqliteOrmLiteDialectProvider();
		}

		[Test]
		public void GetById_checks_cache_first_then_returns_if_found()
		{
			var mockCache = new Mock<ICacheClient>();

			var ormCache = new OrmLitePersistenceProviderCache(mockCache.Object, ConnectionString);

			var row = ModelWithFieldsOfDifferentTypes.Create(1);

			var cacheKey = row.CreateUrn();

			mockCache.Expect(x => x.Get<ModelWithFieldsOfDifferentTypes>(cacheKey))
				.Returns(row);

			var cacheRow = ormCache.GetById<ModelWithFieldsOfDifferentTypes>(row.Id);

			mockCache.VerifyAll();
			ModelWithFieldsOfDifferentTypes.AssertIsEqual(cacheRow, row);
		}

		[Test]
		public void GetById_checks_cache_first_then_gets_from_db()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbCmd = db.CreateCommand())
			{
				dbCmd.CreateTable<ModelWithFieldsOfDifferentTypes>(false);

				var mockCache = new Mock<ICacheClient>();

				var ormCache = new OrmLitePersistenceProviderCache(mockCache.Object, db);

				var row = ModelWithFieldsOfDifferentTypes.Create(1);
				dbCmd.Insert(row);

				var cacheKey = row.CreateUrn();

				mockCache.Expect(x => x.Get<ModelWithFieldsOfDifferentTypes>(cacheKey));

				var dbRow = ormCache.GetById<ModelWithFieldsOfDifferentTypes>(row.Id);

				mockCache.VerifyAll();
				ModelWithFieldsOfDifferentTypes.AssertIsEqual(dbRow, row);
			}
		}

		[Test]
		public void Store_sets_both_cache_and_db()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbCmd = db.CreateCommand())
			{
				dbCmd.CreateTable<ModelWithFieldsOfDifferentTypes>(false);

				var cacheClient = new MemoryCacheClient();

				var ormCache = new OrmLitePersistenceProviderCache(cacheClient, db);

				var row = ModelWithFieldsOfDifferentTypes.Create(1);

				ormCache.Store(row);

				var cacheKey = row.CreateUrn();

				var dbRow = dbCmd.GetById<ModelWithFieldsOfDifferentTypes>(row.Id);
				var cacheRow = cacheClient.Get<ModelWithFieldsOfDifferentTypes>(cacheKey);

				ModelWithFieldsOfDifferentTypes.AssertIsEqual(dbRow, row);
				ModelWithFieldsOfDifferentTypes.AssertIsEqual(cacheRow, row);
			}
		}

		[Test]
		public void Store_sets_and_updates_both_cache_and_db()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbCmd = db.CreateCommand())
			{
				dbCmd.CreateTable<ModelWithFieldsOfDifferentTypes>(false);

				var cacheClient = new MemoryCacheClient();

				var ormCache = new OrmLitePersistenceProviderCache(cacheClient, db);

				var row = ModelWithFieldsOfDifferentTypes.Create(1);

				ormCache.Store(row);

				var cacheKey = row.CreateUrn();
				var dbRow = dbCmd.GetById<ModelWithFieldsOfDifferentTypes>(row.Id);
				var cacheRow = cacheClient.Get<ModelWithFieldsOfDifferentTypes>(cacheKey);

				ModelWithFieldsOfDifferentTypes.AssertIsEqual(dbRow, row);
				ModelWithFieldsOfDifferentTypes.AssertIsEqual(cacheRow, row);

				row.Name = "UpdatedName";
				ormCache.Store(row);

				dbRow = dbCmd.GetById<ModelWithFieldsOfDifferentTypes>(row.Id);
				cacheRow = cacheClient.Get<ModelWithFieldsOfDifferentTypes>(cacheKey);

				ModelWithFieldsOfDifferentTypes.AssertIsEqual(dbRow, row);
				ModelWithFieldsOfDifferentTypes.AssertIsEqual(cacheRow, row);
			}
		}

		[Test]
		public void Clear_only_clears_cache()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbCmd = db.CreateCommand())
			{
				dbCmd.CreateTable<ModelWithFieldsOfDifferentTypes>(false);

				var cacheClient = new MemoryCacheClient();

				var ormCache = new OrmLitePersistenceProviderCache(cacheClient, db);

				var row = ModelWithFieldsOfDifferentTypes.Create(1);

				ormCache.Store(row);

				var cacheKey = row.CreateUrn();

				var dbRow = dbCmd.GetById<ModelWithFieldsOfDifferentTypes>(row.Id);
				var cacheRow = cacheClient.Get<ModelWithFieldsOfDifferentTypes>(cacheKey);

				ModelWithFieldsOfDifferentTypes.AssertIsEqual(dbRow, row);
				ModelWithFieldsOfDifferentTypes.AssertIsEqual(cacheRow, row);

				ormCache.Clear<ModelWithFieldsOfDifferentTypes>(row.Id);

				dbRow = dbCmd.GetById<ModelWithFieldsOfDifferentTypes>(row.Id);
				cacheRow = cacheClient.Get<ModelWithFieldsOfDifferentTypes>(cacheKey);

				ModelWithFieldsOfDifferentTypes.AssertIsEqual(dbRow, row);
				Assert.IsNull(cacheRow);
			}
		}

	}
}