using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.Expression;

public class Custom1
{
    public int Id { get; set; }
    public string Field1 { get; set; }
    public string Field2 { get; set; }

    [CustomSelect(@"(CASE Custom1Id 
            WHEN 1 THEN 'On Hold' 
            WHEN 3 THEN 'In Treatment' 
            WHEN 7 THEN 'Awaiting First Appointment' 
            WHEN 8 THEN 'Discharged' 
            WHEN 12 THEN 'Closed - Awaiting Review' 
            ELSE 'Closed and Reviewed' 
        END)")]
    public string StatusText { get; set; }
}

public class Custom2
{
    public int Id { get; set; }
    public int Custom1Id { get; set; }
    public string Field3 { get; set; }
    public string Field4 { get; set; }
}

[TestFixtureOrmLite]
public class SqlCustomTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    [IgnoreDialect(Dialect.AnyPostgreSql, "Not supported")]
    public void Can_use_CustomSelect_field_in_Typed_Query()
    {
        using (var db = OpenDbConnection())
        {
            db.DropAndCreateTable<Custom1>();
            db.DropAndCreateTable<Custom2>();

            db.Insert(new Custom1 { Id = 1, Field1 = "f1", Field2 = "f2" });
            db.Insert(new Custom2 { Id = 2, Field3 = "f3", Field4 = "f4", Custom1Id = 1 });

            var q = db.From<Custom1>()
                .LeftJoin<Custom2>()
                .Where(x => x.StatusText == "On Hold")
                .Select<Custom1, Custom2>((t1, t2) => new
                {
                    t1,
                    t2,
                });

            var results = db.Select<Dictionary<string, object>>(q);

            Assert.That(results[0]["StatusText"], Is.EqualTo("On Hold"));
        }
    }
}