using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Issues
{
    [Schema("Schema")]
    public class ModelWithSchema
    {
        [AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; }

        public int Value { get; set; }
    }

    [TestFixtureOrmLite]
    public class JoinAliasWithSchemaIssue(DialectContext context) : OrmLiteProvidersTestBase(context)
    {
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            if (!DialectFeatures.SchemaSupport) return;

            using var db = OpenDbConnection();
            db.CreateSchema<ModelWithSchema>();
        }

        [Test]
        public void Can_perform_join_alias_on_ModelWithSchema()
        {
            using var db = OpenDbConnection();
            db.DropAndCreateTable<ModelWithSchema>();

            db.Insert(new ModelWithSchema { Name = "One", Value = 1 });
            db.Insert(new ModelWithSchema { Name = "Uno", Value = 1 });

            var q = db.From<ModelWithSchema>()
                .Join<ModelWithSchema>((a, b) => a.Value == b.Value, db.JoinAlias("b"))
                .Select(x => new
                {
                    AName = x.Name,
                    BName = Sql.JoinAlias(x.Name, "b")
                });

            var results = db.Select<object>(q);
            db.GetLastSql().Print();
            Assert.That(results.Count, Is.EqualTo(2 * 2));
        }
        
        [Test]
        public void Can_perform_table_alias_on_ModelWithSchema()
        {
            using var db = OpenDbConnection();
            db.DropAndCreateTable<ModelWithSchema>();

            db.Insert(new ModelWithSchema { Name = "One", Value = 1 });
            db.Insert(new ModelWithSchema { Name = "Uno", Value = 1 });
            
            db.Select<ModelWithSchema>().PrintDumpTable();

            var q = db.From<ModelWithSchema>()
                .Join<ModelWithSchema>((a, b) => a.Value == b.Value, db.TableAlias("b"))
                .Select(x => new
                {
                    AName = x.Name,
                    BName = Sql.TableAlias(x.Name, "b")
                });

            var results = db.Select<(string AName, string BName)>(q);
            results.PrintDump();
            db.GetLastSql().Print();
            Assert.That(results.Count, Is.EqualTo(2 * 2));
        }
    }
}