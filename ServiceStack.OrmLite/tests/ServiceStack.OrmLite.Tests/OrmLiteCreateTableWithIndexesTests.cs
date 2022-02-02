using NUnit.Framework;
using ServiceStack.Text;
using ServiceStack.Common.Tests.Models;

namespace ServiceStack.OrmLite.Tests
{
    [TestFixtureOrmLite]
    public class OrmLiteCreateTableWithIndexesTests : OrmLiteProvidersTestBase
    {
        public OrmLiteCreateTableWithIndexesTests(DialectContext context) : base(context) {}

        [Test]
        public void Can_create_ModelWithIndexFields_table()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithIndexFields>(true);

                var sql = DialectProvider.ToCreateIndexStatements(typeof(ModelWithIndexFields)).Join();

                var indexName = "idx_modelwithindexfields_name";
                var uniqueName = "uidx_modelwithindexfields_uniquename";

                if ((Dialect.AnyOracle | Dialect.Firebird).HasFlag(Dialect))
                {
                    indexName = DialectProvider.NamingStrategy.ApplyNameRestrictions(indexName);
                    uniqueName = DialectProvider.NamingStrategy.ApplyNameRestrictions(uniqueName);
                }

                Assert.That(sql, Does.Contain(indexName));
                Assert.That(sql, Does.Contain(uniqueName));
                Assert.That(sql, Does.Contain("altname"));
            }
        }

        [Test]
        public void Can_create_ModelWithCompositeIndexFields_table()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithCompositeIndexFields>(true);

                var sql = DialectProvider.ToCreateIndexStatements(typeof(ModelWithCompositeIndexFields)).Join();

                var indexName = "idx_modelwithcompositeindexfields_name";
                var compositeName = "idx_modelwithcompositeindexfields_composite1_composite2";

                if ((Dialect.AnyOracle | Dialect.Firebird).HasFlag(Dialect))
                {
                    indexName = DialectProvider.NamingStrategy.ApplyNameRestrictions(indexName);
                    compositeName = DialectProvider.NamingStrategy.ApplyNameRestrictions(compositeName);
                }

                Assert.That(sql, Does.Contain(indexName));
                Assert.That(sql, Does.Contain(compositeName));
            }
        }

        [Test]
        public void Can_create_ModelWithCompositeIndexFieldsDesc_table()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithCompositeIndexFieldsDesc>(true);
                db.GetLastSql().Print();

                var sql = DialectProvider.ToCreateIndexStatements(typeof(ModelWithCompositeIndexFieldsDesc)).Join();

                var indexName = "idx_modelwithcompositeindexfieldsdesc_name";
                var compositeName = "idx_modelwithcompositeindexfieldsdesc_composite1_composite2";

                if ((Dialect.AnyOracle | Dialect.Firebird).HasFlag(Dialect))
                {
                    indexName = DialectProvider.NamingStrategy.ApplyNameRestrictions(indexName).ToLower();
                    compositeName = DialectProvider.NamingStrategy.ApplyNameRestrictions(compositeName).ToLower();
                }

                Assert.That(sql, Does.Contain(indexName));
                Assert.That(sql, Does.Contain(compositeName));
            }
        }

        [Test]
        [IgnoreDialect(Tests.Dialect.Firebird, "Too long, not supported")]
        public void Can_create_ModelWithCompositeIndexOnFieldSpacesDesc_table()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithCompositeIndexOnFieldSpacesDesc>(true);
                db.GetLastSql().Print();

                var sql = DialectProvider.ToCreateIndexStatements(typeof(ModelWithCompositeIndexOnFieldSpacesDesc)).Join();

                var compositeName = "idx_modelwithcompositeindexonfieldspacesdesc_field_field";

                if ((Dialect.AnyOracle | Dialect.Firebird).HasFlag(Dialect))
                    compositeName = DialectProvider.NamingStrategy.ApplyNameRestrictions(compositeName).ToLower();

                Assert.That(sql, Does.Contain(compositeName));
            }
        }

        [Test]
        public void Can_create_ModelWithNamedCompositeIndex_table()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithNamedCompositeIndex>(true);

                var sql = DialectProvider.ToCreateIndexStatements(typeof(ModelWithNamedCompositeIndex)).Join();

                var indexName = "idx_modelwithnamedcompositeindex_name";
                var compositeName = "uidx_modelwithnamedcompositeindexfields_composite1_composite2";

                if (Dialect == Dialect.AnyOracle || Dialect == Dialect.Firebird)
                {
                    indexName = DialectProvider.NamingStrategy.ApplyNameRestrictions(indexName);
                    compositeName = DialectProvider.NamingStrategy.ApplyNameRestrictions(compositeName);
                }

                Assert.That(sql, Does.Contain(indexName));
                Assert.That(sql, Does.Contain("custom_index_name"));
                Assert.That(sql, Does.Not.Contain(compositeName));
            }
        }

        [Test]
        public void Can_create_CompositeIndex_with_Enum()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<ModelWithEnum>();
            }
        }
    }
}