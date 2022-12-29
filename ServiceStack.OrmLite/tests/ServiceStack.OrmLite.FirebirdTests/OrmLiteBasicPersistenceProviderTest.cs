using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;

namespace ServiceStack.OrmLite.FirebirdTests
{
	[TestFixture]
	public class OrmLiteBasicPersistenceProviderTests
		: OrmLiteTestBase
	{

		[Test]
		public void Can_GetById_from_basic_persistence_provider()
		{
			using (var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open())
			{
				db.DropAndCreateTable<ModelWithFieldsOfDifferentTypes>();

				//var basicProvider = new OrmLitePersistenceProvider(db);

				var row = ModelWithFieldsOfDifferentTypes.Create(1);

				db.Insert(row);

				var providerRow = db.SingleById<ModelWithFieldsOfDifferentTypes>(1);

				ModelWithFieldsOfDifferentTypes.AssertIsEqual(providerRow, row);
			}
		}

		[Test]
		public void Can_GetByIds_from_basic_persistence_provider()
		{
            using (var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open())
			{
				db.DropAndCreateTable<ModelWithFieldsOfDifferentTypes>();

				var basicProvider = new OrmLitePersistenceProvider(db);

				var rowIds = new List<int> { 1, 2, 3, 4, 5 };

				var rows = rowIds.ConvertAll(x => ModelWithFieldsOfDifferentTypes.Create(x));

				rows.ForEach(x => db.Insert(x));

				var getRowIds = new[] { 2, 4 };
				var providerRows = basicProvider.GetByIds<ModelWithFieldsOfDifferentTypes>(getRowIds).ToList();
				var providerRowIds = providerRows.ConvertAll(x => x.Id);

				Assert.That(providerRowIds, Is.EquivalentTo(getRowIds));
			}
		}

		[Test]
		public void Can_Store_from_basic_persistence_provider()
		{
            using (var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open())
			{
				db.DropAndCreateTable<ModelWithFieldsOfDifferentTypes>();

				var basicProvider = new OrmLitePersistenceProvider(db);

				var rowIds = new List<int> { 1, 2, 3, 4, 5 };

				var rows = rowIds.ConvertAll(x => ModelWithFieldsOfDifferentTypes.Create(x));

				rows.ForEach(x => basicProvider.Store(x));

				var getRowIds = new[] { 2, 4 };
				var providerRows = db.SelectByIds<ModelWithFieldsOfDifferentTypes>(getRowIds).ToList();
				var providerRowIds = providerRows.ConvertAll(x => x.Id);

				Assert.That(providerRowIds, Is.EquivalentTo(getRowIds));
			}
		}

		[Test]
		public void Can_Delete_from_basic_persistence_provider()
		{
            using (var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open())
			{
				db.DropAndCreateTable<ModelWithFieldsOfDifferentTypes>();

				var basicProvider = new OrmLitePersistenceProvider(db);

				var rowIds = new List<int> { 1, 2, 3, 4, 5 };

				var rows = rowIds.ConvertAll(x => ModelWithFieldsOfDifferentTypes.Create(x));

				rows.ForEach(x => db.Insert(x));

				var deleteRowIds = new List<int> { 2, 4 };

				foreach (var row in rows)
				{
					if (deleteRowIds.Contains(row.Id))
					{
						basicProvider.Delete(row);
					}
				}

				var providerRows = basicProvider.GetByIds<ModelWithFieldsOfDifferentTypes>(rowIds).ToList();
				var providerRowIds = providerRows.ConvertAll(x => x.Id);

				var remainingIds = new List<int>(rowIds);
				deleteRowIds.ForEach(x => remainingIds.Remove(x));

				Assert.That(providerRowIds, Is.EquivalentTo(remainingIds));
			}
		}

	}

}