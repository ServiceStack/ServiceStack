using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Issues;

[TestFixtureOrmLite]
public class SqlExpressionIssues(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    public class MetadataEntity
    {
        [AutoIncrement]
        public int Id { get; set; }
        public int ObjectTypeCode { get; set; }
        public string LogicalName { get; set; }
    }

    [Test]
    public void Can_Equals_method_and_operator_with_Scalar()
    {
        using (var db = OpenDbConnection())
        {
            db.DropAndCreateTable<MetadataEntity>();

            db.Insert(new MetadataEntity {ObjectTypeCode = 1, LogicalName = "inno_subject"});
                
            Assert.That(db.Scalar<MetadataEntity, int>(e => e.ObjectTypeCode, e => e.LogicalName == "inno_subject"), Is.EqualTo(1));
            Assert.That(db.Scalar<MetadataEntity, int>(e => e.ObjectTypeCode, e => e.LogicalName.Equals("inno_subject")), Is.EqualTo(1));
        }
    }
    
    public class TestExpression
    {
        public int Id { get; set; }
        public bool? BooleanColumn { get; set; }
        public string Name { get; set; } = "";
    }

    [Test]
    public void Should_Generate_Correct_Coalesce_Boolean_Statement()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<TestExpression>();
        // OrmLiteUtils.PrintSql();
        
        var query = db.From<TestExpression>()
            .Select(x => new {
                Id = x.Id,
                Result = x.BooleanColumn ?? true,
                Name = x.Name
            })
            .Where(x => x.BooleanColumn ?? true);

        db.DropAndCreateTable<TestExpression>();
        var tableItem = new TestExpression { Id = 1, BooleanColumn = null, Name = "Test Record" };

        db.Insert(tableItem);

        var result = db.Select(query);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result.First().Name, Is.EqualTo(tableItem.Name));
    }
    
    [Test]
    public void Should_Generate_expression_with_mod()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<TestExpression>();
        // OrmLiteUtils.PrintSql();

        var query = db.From<TestExpression>()
            .Select(x => new {
                Id = x.Id,
                ModResult = x.Id % 2,
                Name = x.Name
            });

        var sql = query.ToSelectStatement();
        sql.Print();

        db.DropAndCreateTable<TestExpression>();
        var tableItem = new TestExpression { Id = 10, BooleanColumn = null, Name = "Test Record" };

        db.Insert(tableItem);

        var result = db.Select<(int Id, int ModResult, string Name)>(query);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(1));

        Assert.That(result.First().ModResult, Is.EqualTo(0));

        Assert.That(sql, Does.Contain("MOD").Or.Contain("%"));
        Assert.That(sql, Does.Not.Contain("COALESCE"));
    }
}