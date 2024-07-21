using System;
using System.Collections.Generic;
using System.Data;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.UseCase;

public interface ITestPoco
{
    int Id { get; set; }

    DateTime DateCreated { get; set; }
}

public class TestPocoImpl : ITestPoco
{
    [AutoIncrement]
    public int Id { get; set; }

    public DateTime DateCreated { get; set; }

    public string Name { get; set; }
}

public class EntityAttribute
{
    [AutoIncrement]
    public int Id { get; set; }

    public int EntityId { get; set; }

    [Required, StringLength(100)]
    public string EntityType { get; set; }

    [Required, StringLength(100)]
    public string AttributeName { get; set; }

    [Required, StringLength(100)]
    public string AttributeValue { get; set; }
}

[TestFixtureOrmLite]
public class FieldFromInterfaceImplementationUseCase(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public void Can_select_on_generic_interface_implementation_properties_with_PrefixFieldWithTableName()
    {
        using (var db = OpenDbConnection())
        {
            db.DropAndCreateTable<TestPocoImpl>();

            var testPoco = new TestPocoImpl { DateCreated = DateTime.Now, Name = "Object 1" };

            testPoco.Id = (int)db.Insert<TestPocoImpl>(testPoco, true);

            var rows = GetRows<TestPocoImpl>(db, testPoco.Id);

            Assert.That(rows.Count, Is.EqualTo(1));
        }
    }

    [Test]
    public void Can_select_on_generic_interface_implementation_properties_with_join()
    {
        using (var db = OpenDbConnection())
        {
            db.DropAndCreateTable<TestPocoImpl>();
            db.DropAndCreateTable<EntityAttribute>();

            var testPoco = new TestPocoImpl { DateCreated = DateTime.Now, Name = "Object 1" };

            testPoco.Id = (int)db.Insert<TestPocoImpl>(testPoco, true);

            var entityAttribute = new EntityAttribute { EntityType = "TestPocoImpl", EntityId = testPoco.Id, AttributeName = "Description", AttributeValue = "Some Object" };

            db.Insert<EntityAttribute>(entityAttribute);

            var rows = GetRowsByIdWhereHasAnyAttributes<TestPocoImpl>(db, testPoco.Id);

            Assert.That(rows.Count, Is.EqualTo(1));
        }
    }

    private List<T> GetRows<T>(IDbConnection db, int id)
        where T : ITestPoco
    {
        var ev = db.From<T>();

        ev.PrefixFieldWithTableName = true;

        ev.Where(x => x.Id == id);

        return db.Select<T>(ev);
    }

    private List<T> GetRowsByIdWhereHasAnyAttributes<T>(IDbConnection db, int id)
        where T : ITestPoco
    {
        var typeName = typeof(T).Name;

        var ev = db.From<T, EntityAttribute>((x, y) => x.Id == y.EntityId && y.EntityType == typeName);

        ev.Select(x => new { x.Id, x.DateCreated })
            .Where(x => x.Id == id);

        return db.Select<T>(ev);
    }
}