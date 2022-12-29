using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.OrmLite.Tests;

namespace ServiceStack.OrmLite.PostgreSQL.Tests
{
    public class PostgreSqlTypes
    {
        public int Id { get; set; }

        //hstore
        public Dictionary<string,string> Dictionary { get; set; }
        public IDictionary<string,string> IDictionary { get; set; }
    }
    
    [TestFixtureOrmLiteDialects(Dialect.AnyPostgreSql)]
    public class PostgreSqlDataTypesTests : OrmLiteProvidersTestBase
    {
        public PostgreSqlDataTypesTests(DialectContext context) : base(context) {}

        [OneTimeSetUp] 
        public void OneTimeSetup()
        {
            PostgreSqlDialectProvider.Instance.UseHstore = true;
            using (var db = OpenDbConnection())
            {
                db.ExecuteSql("CREATE EXTENSION IF NOT EXISTS hstore;");
            }
        }

        [OneTimeTearDown] public void OneTimeTearDown() => PostgreSqlDialectProvider.Instance.UseHstore = false;

        [Test]
        public void Does_save_string_dictionary_in_hstore_columns()
        {
            using (var db = OpenDbConnection())
            {
                var sb = OrmLiteUtils.CaptureSql();
                
                db.DropAndCreateTable<PostgreSqlTypes>();
                
                Assert.That(OrmLiteUtils.UnCaptureSqlAndFree(sb), Does.Contain("hstore"));
                
                db.Insert(new PostgreSqlTypes
                {
                    Id = 1,
                    Dictionary = new Dictionary<string, string> { {"A", "1"} },
                    IDictionary = new Dictionary<string, string> { {"B", "2"} },
                });
                
                Assert.That(db.Single(db.From<PostgreSqlTypes>().Where("dictionary -> 'A' = '1'")).Id, 
                    Is.EqualTo(1));

                var q = db.From<PostgreSqlTypes>();
                Assert.That(db.Single(q.Where($"{q.Column<PostgreSqlTypes>(x => x.IDictionary)} -> 'B' = '2'")).Id, 
                    Is.EqualTo(1));
            }
        }
    }
}