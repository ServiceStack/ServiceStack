using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.Issues;

[Schema("Schema")]
public class Entity1
{
    public int Id { get; set; }

    public int Entity2Fk { get; set; }
}

[Schema("Schema")]
public class Entity2
{
    public int Id { get; set; }
}

public class PlainModel
{
    public int Entity1Id { get; set; }

    public int Entity2Id { get; set; }
}

[TestFixtureOrmLite]
public class JoinsWithSchemas(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        if (!DialectFeatures.SchemaSupport) return;

        using var db = OpenDbConnection();
        db.CreateSchema<ModelWithSchema>();
    }

    [Test]
    public void Can_detect_if_table_with_schema_exists()
    {
        using var db = OpenDbConnection();
        db.DropTable<Entity1>();
        db.DropTable<Entity2>();

        var exists = db.TableExists<Entity1>();
        Assert.That(exists, Is.False);

        exists = db.TableExists<Entity2>();
        Assert.That(exists, Is.False);

        db.CreateTable<Entity1>();
        db.CreateTable<Entity2>();

        exists = db.TableExists<Entity1>();
        Assert.That(exists, Is.True);

        exists = db.TableExists<Entity2>();
        Assert.That(exists, Is.True);
    }

    [Test]
    public void Can_join_entities_with_Schema()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Entity1>();
        db.DropAndCreateTable<Entity2>();

        db.Insert(new Entity2 { Id = 1 });
        db.Insert(new Entity1 { Id = 2, Entity2Fk = 1 });

        var results = db.Select<PlainModel>(
            db.From<Entity1>()
                .Join<Entity2>((e1, e2) => e1.Entity2Fk == e2.Id));

        Assert.That(results.Count, Is.EqualTo(1));
        Assert.That(results[0].Entity1Id, Is.EqualTo(2));
        Assert.That(results[0].Entity2Id, Is.EqualTo(1));
    }
}