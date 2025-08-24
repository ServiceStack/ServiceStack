using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.OrmLite.Tests;

namespace ServiceStack.OrmLite.PostgreSQL.Tests
{

    [TestFixtureOrmLiteDialects(Dialect.AnyPostgreSql)]
    public class OrmLiteCreateTableWithNamingStrategyTests : OrmLiteProvidersTestBase
    {
        public OrmLiteCreateTableWithNamingStrategyTests(DialectContext context) : base(context) {}

        [Test]
        public void Can_create_TableWithNamingStrategy_table_prefix()
        {
            using (new TemporaryNamingStrategy(DialectProvider, new PrefixNamingStrategy { TablePrefix = "tab_", ColumnPrefix = "col_" }))
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithOnlyStringFields>(true);
            }
        }

        [Test]
        public void Can_create_TableWithNamingStrategy_table_lowered()
        {
            using (new TemporaryNamingStrategy(DialectProvider, new LowercaseNamingStrategy()))
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithOnlyStringFields>(true);
            }
        }


        [Test]
        public void Can_create_TableWithNamingStrategy_table_nameUnderscoreCompound()
        {
            using (new TemporaryNamingStrategy(DialectProvider, new UnderscoreSeparatedCompoundNamingStrategy()))
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithOnlyStringFields>(true);
            }
        }

        [Test]
        public void Can_get_data_from_TableWithNamingStrategy_with_GetById()
        {
            using (new TemporaryNamingStrategy(DialectProvider, new PrefixNamingStrategy { TablePrefix = "tab_", ColumnPrefix = "col_" }))
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithOnlyStringFields>(true);
                ModelWithOnlyStringFields m = new ModelWithOnlyStringFields() { Id = "999", AlbumId = "112", AlbumName = "ElectroShip", Name = "MyNameIsBatman" };

                db.Save<ModelWithOnlyStringFields>(m);
                var modelFromDb = db.SingleById<ModelWithOnlyStringFields>("999");

                Assert.AreEqual(m.Name, modelFromDb.Name);
            }
        }


        [Test]
        public void Can_get_data_from_TableWithNamingStrategy_with_query_by_example()
        {
            using (new TemporaryNamingStrategy(DialectProvider, new PrefixNamingStrategy { TablePrefix = "tab_", ColumnPrefix = "col_" }))
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithOnlyStringFields>(true);
                ModelWithOnlyStringFields m = new ModelWithOnlyStringFields() { Id = "998", AlbumId = "112", AlbumName = "ElectroShip", Name = "QueryByExample" };

                db.Save<ModelWithOnlyStringFields>(m);
                var modelFromDb = db.Where<ModelWithOnlyStringFields>(new { Name = "QueryByExample" })[0];

                Assert.AreEqual(m.Name, modelFromDb.Name);
            }
        }
        
        
        [Test]
        public void Can_get_data_from_TableWithNamingStrategy_AfterChangingNamingStrategy()
        {
            using (new TemporaryNamingStrategy(DialectProvider, new PrefixNamingStrategy { TablePrefix = "tab_", ColumnPrefix = "col_" }))
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithOnlyStringFields>(true);
                ModelWithOnlyStringFields m = new ModelWithOnlyStringFields() { Id = "998", AlbumId = "112", AlbumName = "ElectroShip", Name = "QueryByExample" };

                db.Save<ModelWithOnlyStringFields>(m);
                var modelFromDb = db.Where<ModelWithOnlyStringFields>(new { Name = "QueryByExample" })[0];

                Assert.AreEqual(m.Name, modelFromDb.Name);

                modelFromDb = db.SingleById<ModelWithOnlyStringFields>("998");
                Assert.AreEqual(m.Name, modelFromDb.Name);
                
            }
            
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithOnlyStringFields>(true);
                ModelWithOnlyStringFields m = new ModelWithOnlyStringFields() { Id = "998", AlbumId = "112", AlbumName = "ElectroShip", Name = "QueryByExample" };

                db.Save<ModelWithOnlyStringFields>(m);
                var modelFromDb = db.Where<ModelWithOnlyStringFields>(new { Name = "QueryByExample" })[0];

                Assert.AreEqual(m.Name, modelFromDb.Name);

                modelFromDb = db.SingleById<ModelWithOnlyStringFields>("998");
                Assert.AreEqual(m.Name, modelFromDb.Name);	
            }
            
            using (new TemporaryNamingStrategy(DialectProvider, new PrefixNamingStrategy { TablePrefix = "tab_", ColumnPrefix = "col_" }))
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithOnlyStringFields>(true);
                ModelWithOnlyStringFields m = new ModelWithOnlyStringFields() { Id = "998", AlbumId = "112", AlbumName = "ElectroShip", Name = "QueryByExample" };

                db.Save<ModelWithOnlyStringFields>(m);
                var modelFromDb = db.Where<ModelWithOnlyStringFields>(new { Name = "QueryByExample" })[0];

                Assert.AreEqual(m.Name, modelFromDb.Name);

                modelFromDb = db.SingleById<ModelWithOnlyStringFields>("998");
                Assert.AreEqual(m.Name, modelFromDb.Name);
            }
        }

    }

    internal class PrefixNamingStrategy : OrmLiteNamingStrategyBase
    {

        public string TablePrefix { get; set; }

        public string ColumnPrefix { get; set; }

        public override string GetTableName(string name)
        {
            return TablePrefix + name;
        }

        public override string GetColumnName(string name)
        {
            return ColumnPrefix + name;
        }

    }

    internal class LowercaseNamingStrategy : OrmLiteNamingStrategyBase
    {

        public override string GetTableName(string name)
        {
            return name.ToLower();
        }

        public override string GetColumnName(string name)
        {
            return name.ToLower();
        }

    }

    internal class UnderscoreSeparatedCompoundNamingStrategy : OrmLiteNamingStrategyBase
    {

        public override string GetTableName(string name)
        {
            return toUnderscoreSeparatedCompound(name);
        }

        public override string GetColumnName(string name)
        {
            return toUnderscoreSeparatedCompound(name);
        }


        string toUnderscoreSeparatedCompound(string name)
        {

            string r = char.ToLower(name[0]).ToString();

            for (int i = 1; i < name.Length; i++)
            {
                char c = name[i];
                if (char.IsUpper(name[i]))
                {
                    r += "_";
                    r += char.ToLower(name[i]);
                }
                else
                {
                    r += name[i];
                }
            }
            return r;
        }

    }

}