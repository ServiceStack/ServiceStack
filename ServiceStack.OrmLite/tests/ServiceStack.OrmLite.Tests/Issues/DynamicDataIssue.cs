using System;
using System.Collections.Generic;
using System.Data;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Issues;

public class ParentTbl
{
    [AutoIncrement]
    public long Id { get; set; }

    public DateTime? DateMarried { get; set; }

    [Reference]
    public List<ChildTbl> Childs { get; set; } = [];

    public DateTime? DateOfBirth { get; set; }
}

public class ChildTbl
{
    [AutoIncrement]
    public long Id { get; set; }

    [References(typeof(ParentTbl))]
    public long ParentId { get; set; }

    public DateTime? DateOfDeath { get; set; }
}

[TestFixtureOrmLite]
public class DynamicDataIssue(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    private static void InitTables(IDbConnection db)
    {
        db.DropTable<ChildTbl>();
        db.DropTable<ParentTbl>();
        db.CreateTable<ParentTbl>();
        db.CreateTable<ChildTbl>();
    }

    [Test] 
    public void Can_select_null_DateTime_in_nullable_Tuple()
    {
        var date = new DateTime(2000,1,1);

        using var db = OpenDbConnection();
        InitTables(db);

        db.Insert(new ParentTbl { DateOfBirth = date });
        db.Insert(new ParentTbl { DateOfBirth = null });

        db.Select<ParentTbl>();
        db.Select<(int, DateTime?)>(db.From<ParentTbl>());
        db.Select<(int, DateTime?)>(db.From<ParentTbl>().Select(x => new { x.Id, x.DateOfBirth }));
        db.Select<(int, DateTime)>(db.From<ParentTbl>().Select(x => new { x.Id, x.DateOfBirth }));
    }

    [Test]
    public void Complex_example()
    {
        using var db = OpenDbConnection();
        InitTables(db);

        var parentTbl = new ParentTbl { DateMarried = DateTime.Today };
        parentTbl.Id = db.Insert(parentTbl, selectIdentity:true);
        db.Insert(new ChildTbl { ParentId = parentTbl.Id, DateOfDeath = null });

        var q = db.From<ChildTbl>()
            .RightJoin<ChildTbl, ParentTbl>((c, p) => c.ParentId == p.Id || c.Id == null)
            .GroupBy<ParentTbl>((p) => new { p.Id })
            .Select<ChildTbl, ParentTbl>((c, p) => new { p.Id, MaxKeyValuePeriodEnd = Sql.Max(c.DateOfDeath) });

        var theSqlStatement = q.ToSelectStatement();
                
        theSqlStatement.Print();
    }
}