using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;

namespace ServiceStack.OrmLite.Tests
{
    [TestFixtureOrmLite]
    public class OrmLiteBasicPersistenceProviderTests : OrmLiteProvidersTestBase
    {
        public OrmLiteBasicPersistenceProviderTests(DialectContext context) : base(context) {}

        [Test]
        public void Can_GetById_from_basic_persistence_provider()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithFieldsOfDifferentTypes>(true);

                var basicProvider = new OrmLitePersistenceProvider(db);

                var row = ModelWithFieldsOfDifferentTypes.Create(1);

                row.Id = (int)db.Insert(row, selectIdentity: true);

                var providerRow = basicProvider.GetById<ModelWithFieldsOfDifferentTypes>(row.Id);

                ModelWithFieldsOfDifferentTypes.AssertIsEqual(providerRow, row);
            }
        }

        [Test]
        public void Can_GetByIds_from_basic_persistence_provider()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithFieldsOfDifferentTypes>(true);

                var basicProvider = new OrmLitePersistenceProvider(db);

                var rowIds = new List<int> { 1, 2, 3, 4, 5 };

                var rows = rowIds.ConvertAll(ModelWithFieldsOfDifferentTypes.Create);

                rows.ForEach(x => { x.Id = (int)db.Insert(x, selectIdentity: true); });

                var getRowIds = new[] { rows[1].Id, rows[3].Id };
                var providerRows = basicProvider.GetByIds<ModelWithFieldsOfDifferentTypes>(getRowIds).ToList();
                var providerRowIds = providerRows.ConvertAll(x => x.Id);

                Assert.That(providerRowIds, Is.EquivalentTo(getRowIds));
            }
        }

        [Test]
        public void Can_Store_from_basic_persistence_provider()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithFieldsOfDifferentTypes>(true);

                var basicProvider = new OrmLitePersistenceProvider(db);

                var rowIds = new List<int> { 1, 2, 3, 4, 5 };

                var rows = rowIds.ConvertAll(ModelWithFieldsOfDifferentTypes.Create);

                rows.ForEach(x => basicProvider.Store(x));

                var getRowIds = new[] { rows[1].Id, rows[3].Id };
                var providerRows = db.SelectByIds<ModelWithFieldsOfDifferentTypes>(getRowIds).ToList();
                var providerRowIds = providerRows.ConvertAll(x => x.Id);

                Assert.That(providerRowIds, Is.EquivalentTo(getRowIds));
            }
        }

        [Test]
        public void Can_Delete_from_basic_persistence_provider()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithFieldsOfDifferentTypes>(true);

                var basicProvider = new OrmLitePersistenceProvider(db);

                var rowIds = new List<int> { 1, 2, 3, 4, 5 };

                var rows = rowIds.ConvertAll(ModelWithFieldsOfDifferentTypes.Create);

                rows.ForEach(x => { x.Id = (int)db.Insert(x, selectIdentity: true); });

                var deleteRowIds = new List<int> { rows[1].Id, rows[3].Id };
                var getRowIds = rows.ConvertAll(x => x.Id);

                rows.Where(row => deleteRowIds.Contains(row.Id)).ToList().ForEach(basicProvider.Delete);

                var providerRows = basicProvider.GetByIds<ModelWithFieldsOfDifferentTypes>(getRowIds).ToList();
                var providerRowIds = providerRows.ConvertAll(x => x.Id);

                var remainingIds = rows.ConvertAll(x => x.Id);
                deleteRowIds.ForEach(x => remainingIds.Remove(x));

                Assert.That(providerRowIds, Is.EquivalentTo(remainingIds));
            }
        }

    }
}