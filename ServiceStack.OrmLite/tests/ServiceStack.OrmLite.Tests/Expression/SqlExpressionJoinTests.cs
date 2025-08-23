using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Expression;

[TestFixtureOrmLite]
public class SqlExpressionJoinTests(DialectContext context) : ExpressionsTestBase(context)
{
    public class TableA
    {
        public int Id { get; set; }
        public bool Bool { get; set; }
        public string Name { get; set; }
    }

    public class TableB
    {
        public int Id { get; set; }
        public int TableAId { get; set; }
        public string Name { get; set; }
    }

    [Test]
    public void Can_query_bools()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<TableA>();
        db.DropAndCreateTable<TableB>();

        db.Insert(new TableA { Id = 1, Bool = false });
        db.Insert(new TableA { Id = 2, Bool = true });
        db.Insert(new TableB { Id = 1, TableAId = 1 });
        db.Insert(new TableB { Id = 2, TableAId = 2 });

        var q = db.From<TableA>()
            .LeftJoin<TableB>((a, b) => a.Id == b.Id)
            .Where(a => !a.Bool);

        var result = db.Single(q);
        var lastSql = db.GetLastSql();
        lastSql.Print();
        Assert.That(result.Id, Is.EqualTo(1));
        Assert.That(lastSql, Is.Not.Contains("NOT"));

        q = db.From<TableA>()
            .Where(a => !a.Bool)
            .LeftJoin<TableB>((a, b) => a.Id == b.Id);

        result = db.Single(q);
        lastSql = db.GetLastSql();
        lastSql.Print();
        Assert.That(result.Id, Is.EqualTo(1));
        Assert.That(lastSql, Is.Not.Contains("NOT"));


        q = db.From<TableA>()
            .Where(a => !a.Bool);

        result = db.Single(q);
        lastSql = db.GetLastSql();
        lastSql.Print();
        Assert.That(result.Id, Is.EqualTo(1));
        Assert.That(lastSql, Is.Not.Contains("NOT"));

        q = db.From<TableA>()
            .LeftJoin<TableB>((a, b) => a.Id == b.Id)
            .Where(a => a.Bool);

        result = db.Single(q);
        db.GetLastSql().Print();
        Assert.That(result.Id, Is.EqualTo(2));

        q = db.From<TableA>()
            .Where(a => a.Bool)
            .LeftJoin<TableB>((a, b) => a.Id == b.Id);

        result = db.Single(q);
        db.GetLastSql().Print();
        Assert.That(result.Id, Is.EqualTo(2));


        q = db.From<TableA>()
            .Where(a => a.Bool);

        result = db.Single(q);
        db.GetLastSql().Print();
        Assert.That(result.Id, Is.EqualTo(2));
    }

    [Test]
    public void Can_order_by_Joined_table()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<TableA>();
        db.DropAndCreateTable<TableB>();

        db.Insert(new TableA { Id = 1, Bool = false });
        db.Insert(new TableA { Id = 2, Bool = true });
        db.Insert(new TableB { Id = 1, TableAId = 1, Name = "Z" });
        db.Insert(new TableB { Id = 2, TableAId = 2, Name = "A" });

        var q = db.From<TableA>()
            .Join<TableB>()
            .OrderBy(x => x.Id);

        var rows = db.Select(q);
        db.GetLastSql().Print();
        Assert.That(rows.Map(x => x.Id), Is.EqualTo(new[] { 1, 2 }));


        q = db.From<TableA>()
            .Join<TableB>()
            .OrderBy<TableB>(x => x.Name);

        rows = db.Select(q);
        db.GetLastSql().Print();
        Assert.That(rows.Map(x => x.Id), Is.EqualTo(new[] { 2, 1 }));
    }

    [Test]
    public void Can_find_missing_rows_from_Left_Join_on_int_primary_key()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<TableA>();
        db.DropAndCreateTable<TableB>();

        db.Insert(new TableA { Id = 1, Bool = true, Name = "A" });
        db.Insert(new TableA { Id = 2, Bool = true, Name = "B" });
        db.Insert(new TableA { Id = 3, Bool = true, Name = "C" });
        db.Insert(new TableB { Id = 1, TableAId = 1, Name = "Z" });

#pragma warning disable 472
        var missingNames = db.Column<string>(
            db.From<TableA>()
                .LeftJoin<TableB>((a, b) => a.Id == b.Id)
                .Where<TableB>(b => b.Id == null)
                .Select(a => a.Name));
#pragma warning restore 472

        Assert.That(missingNames, Is.EquivalentTo(new[] { "B", "C" }));
    }

    public class CrossJoinTableA
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class CrossJoinTableB
    {
        public int Id { get; set; }
        public int Value { get; set; }
    }

    public class CrossJoinResult
    {
        public int CrossJoinTableAId { get; set; }
        public string Name { get; set; }
        public int CrossJoinTableBId { get; set; }
        public int Value { get; set; }

        public override bool Equals(object obj)
        {
            var other = obj as CrossJoinResult;
            if (other == null)
                return false;

            return CrossJoinTableAId == other.CrossJoinTableAId && string.Equals(Name, other.Name) && CrossJoinTableBId == other.CrossJoinTableBId && Value == other.Value;
        }

        public override int GetHashCode() =>
            (((23*37 + CrossJoinTableAId) * 37 + CrossJoinTableBId) * 37 + Value)
            ^ Name.GetHashCode();
    }

    [Test]
    public void Can_perform_a_crossjoin_without_a_join_expression()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<CrossJoinTableA>();
        db.DropAndCreateTable<CrossJoinTableB>();

        db.Insert(new CrossJoinTableA { Id = 1, Name = "Foo" });
        db.Insert(new CrossJoinTableA { Id = 2, Name = "Bar" });
        db.Insert(new CrossJoinTableB { Id = 5, Value = 3 });
        db.Insert(new CrossJoinTableB { Id = 6, Value = 42 });

        var q = db.From<CrossJoinTableA>()
            .CrossJoin<CrossJoinTableB>()
            .OrderBy<CrossJoinTableA>(x => x.Id)
            .ThenBy<CrossJoinTableB>(x => x.Id);
        var result = db.Select<CrossJoinResult>(q);

        db.GetLastSql().Print();

        Assert.That(result.Count, Is.EqualTo(4));
        var expected = new List<CrossJoinResult>
        {
            new CrossJoinResult { CrossJoinTableAId = 1, Name = "Foo", CrossJoinTableBId = 5, Value = 3 },
            new CrossJoinResult { CrossJoinTableAId = 1, Name = "Foo", CrossJoinTableBId = 6, Value = 42 },
            new CrossJoinResult { CrossJoinTableAId = 2, Name = "Bar", CrossJoinTableBId = 5, Value = 3},
            new CrossJoinResult { CrossJoinTableAId = 2, Name = "Bar", CrossJoinTableBId = 6, Value = 42},
        };
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [Test]
    public void Can_perform_a_crossjoin_with_a_join_expression()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<CrossJoinTableA>();
        db.DropAndCreateTable<CrossJoinTableB>();

        db.Insert(new CrossJoinTableA { Id = 1, Name = "Foo" });
        db.Insert(new CrossJoinTableA { Id = 2, Name = "Bar" });
        db.Insert(new CrossJoinTableB { Id = 5, Value = 3 });
        db.Insert(new CrossJoinTableB { Id = 6, Value = 42 });
        db.Insert(new CrossJoinTableB { Id = 7, Value = 56 });

        var q = db.From<CrossJoinTableA>().CrossJoin<CrossJoinTableB>((a, b) => b.Id > 5 && a.Id < 2).OrderBy<CrossJoinTableA>(x => x.Id).ThenBy<CrossJoinTableB>(x => x.Id);
        var result = db.Select<CrossJoinResult>(q);

        db.GetLastSql().Print();

        Assert.That(result.Count, Is.EqualTo(2));
        var expected = new List<CrossJoinResult>
        {
            new CrossJoinResult { CrossJoinTableAId = 1, Name = "Foo", CrossJoinTableBId = 6, Value = 42 },
            new CrossJoinResult { CrossJoinTableAId = 1, Name = "Foo", CrossJoinTableBId = 7, Value = 56 },
        };
        Assert.That(result, Is.EquivalentTo(expected));
    }

    class JoinTest
    {
        public int Id { get; set; }
    }

    class JoinTestChild
    {
        public int Id { get; set; }

        public int ParentId { get; set; }

        public bool IsActive { get; set; }
    }

    [Test]
    public void Issue_Bool_JoinTable_Expression()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<JoinTest>();
        db.DropAndCreateTable<JoinTestChild>();

        db.InsertAll([
            new JoinTest { Id = 1, },
            new JoinTest { Id = 2, }
        ]);

        db.InsertAll([
            new JoinTestChild
            {
                Id = 1,
                ParentId = 1,
                IsActive = true
            },
            new JoinTestChild
            {
                Id = 2,
                ParentId = 2,
                IsActive = false
            }
        ]);

        var q = db.From<JoinTestChild>();
        q.Where(x => !x.IsActive);
        Assert.That(db.Select(q).Count, Is.EqualTo(1));

        var qSub = db.From<JoinTest>();
        qSub.Join<JoinTestChild>((x, y) => x.Id == y.ParentId);
        qSub.Where<JoinTestChild>(x => !x.IsActive); // This line is a bug!
        Assert.That(db.Select(qSub).Count, Is.EqualTo(1));
    }

    public class Invoice
    {
        public int Id { get; set; }

        public int WorkflowId { get; set; }

        public int DocumentId { get; set; }

        public int PageCount { get; set; }

        public string DocumentStatus { get; set; }

        public string Extra { get; set; }
    }

    public class UsagePageInvoice
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
    }

    [Test]
    public void Can_select_individual_columns()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Invoice>();
        db.DropAndCreateTable<UsagePageInvoice>();

        db.Insert(new Invoice
        {
            Id = 1,
            WorkflowId = 2,
            DocumentId = 3,
            PageCount = 4,
            DocumentStatus = "a",
            Extra = "EXTRA"
        });

#pragma warning disable 472
        var q = db.From<Invoice>()
            .LeftJoin<Invoice, UsagePageInvoice>((i, upi) => i.Id == upi.InvoiceId)
            .Where<Invoice>(i => (i.DocumentStatus == "a" || i.DocumentStatus == "b"))
            .And<UsagePageInvoice>(upi => upi.Id == null)
            .Select(c => new { c.Id, c.WorkflowId, c.DocumentId, c.DocumentStatus, c.PageCount });
#pragma warning restore 472

        var result = db.Select(q).First();

        Assert.That(result.Id, Is.EqualTo(1));
        Assert.That(result.WorkflowId, Is.EqualTo(2));
        Assert.That(result.DocumentId, Is.EqualTo(3));
        Assert.That(result.PageCount, Is.EqualTo(4));
        Assert.That(result.DocumentStatus, Is.EqualTo("a"));
        Assert.That(result.Extra, Is.Null);
    }

    private class JoinSelectResults1
    {
        // From TableA
        public int Id { get; set; }
        public bool Bool { get; set; }
        public string Name { get; set; }

        // From TableB
        public int TableAId { get; set; }

        public override bool Equals(object obj)
        {
            var other = (JoinSelectResults1)obj;
            return Id == other.Id && Bool == other.Bool && Name == other.Name && TableAId == other.TableAId;
        }

        public override int GetHashCode() =>
            ((23*37 + Id) * 37 + Name.GetHashCode())
            ^ (Bool ? 65535 : 0);
    }

    private class JoinSelectResults2
    {
        // From TableA
        public int Id { get; set; }
        public bool Bool { get; set; }
        public string Name { get; set; }

        // From TableB
        public int TableBId { get; set; }
        public string TableBName { get; set; }

        public override bool Equals(object obj)
        {
            var other = (JoinSelectResults2)obj;
            return Id == other.Id && Bool == other.Bool && Name == other.Name && TableBId == other.TableBId && TableBName == other.TableBName;
        }

        public override int GetHashCode() =>
            ((23*37 + Id) * 37 + TableBId)
            ^ Name.GetHashCode()
            ^ TableBName.GetHashCode()
            ^ (Bool ? 65535 : 0);

    }

    private class JoinSelectResults3
    {
        // From TableA
        public int TableA_Id { get; set; }
        public bool TableA_Bool { get; set; }
        public string TableA_Name { get; set; }

        // From TableB
        public int Id { get; set; }
        public string TableBName { get; set; }

        public override bool Equals(object obj)
        {
            var other = (JoinSelectResults3)obj;
            return TableA_Id == other.TableA_Id 
                   && TableA_Bool == other.TableA_Bool 
                   && TableA_Name == other.TableA_Name
                   && Id == other.Id 
                   && TableBName == other.TableBName;
        }

        public override int GetHashCode() =>
            ((23*37 + TableA_Id) * 37 + Id) 
            ^ TableA_Name.GetHashCode() 
            ^ TableBName.GetHashCode() 
            ^ (TableA_Bool ? 65535 : 0);
    }

    [Test]
    public void Can_select_entire_tables()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<TableA>();
        db.DropAndCreateTable<TableB>();

        db.Insert(new TableA { Id = 1, Bool = false, Name = "NameA1" });
        db.Insert(new TableA { Id = 2, Bool = true, Name = "NameA2" });
        db.Insert(new TableB { Id = 1, TableAId = 1, Name = "NameB1" });
        db.Insert(new TableB { Id = 2, TableAId = 2, Name = "NameB2" });
        db.Insert(new TableB { Id = 3, TableAId = 2, Name = "NameB3" });

        try
        {
            // Select all columns from TableA

            var q1 = db.From<TableA>()
                .Join<TableB>()
                .Select<TableA, TableB>((a, b) => new { a, b.TableAId })
                .OrderBy(x => x.Id);

            var rows1 = db.Select<JoinSelectResults1>(q1);
            var expected1 = new[]
            {
                new JoinSelectResults1 { Id = 1, Bool = false, Name = "NameA1", TableAId = 1 },
                new JoinSelectResults1 { Id = 2, Bool = true, Name = "NameA2", TableAId = 2 },
                new JoinSelectResults1 { Id = 2, Bool = true, Name = "NameA2", TableAId = 2 },
            };
            Assert.That(rows1, Is.EqualTo(expected1));

            // Same, but use column aliases for some columns from TableB whose names would conflict otherwise

            var q2 = db.From<TableA>()
                .Join<TableB>()
                .Select<TableA, TableB>((a, b) => new { a, TableBId = b.Id, TableBName = b.Name });

            var rows2 = db.Select<JoinSelectResults2>(q2).OrderBy(r => r.Id).ThenBy(r => r.TableBId);
            var expected2 = new[]
            {
                new JoinSelectResults2 { Id = 1, Bool = false, Name = "NameA1", TableBId = 1, TableBName = "NameB1" },
                new JoinSelectResults2 { Id = 2, Bool = true, Name = "NameA2", TableBId = 2, TableBName = "NameB2" },
                new JoinSelectResults2 { Id = 2, Bool = true, Name = "NameA2", TableBId = 3, TableBName = "NameB3" },
            };
            Assert.That(rows2, Is.EqualTo(expected2));

            // Use column alias prefixes for all columns in TableA
            
            db.Select<TableA>().PrintDumpTable();
            db.Select<TableB>().PrintDumpTable();
            
            var q3 = db.From<TableA>()
                .Join<TableB>()
                .Select<TableA, TableB>((a, b) => new { TableA_ = a, b.Id, TableBName = b.Name });

            var rows3 = db.Select<JoinSelectResults3>(q3)
                .OrderBy(r => r.TableA_Id)
                .ThenBy(r => r.Id).ToList();
            var expected3 = new[]
            {
                new JoinSelectResults3 { TableA_Id = 1, TableA_Bool = false, TableA_Name = "NameA1", Id = 1, TableBName = "NameB1" },
                new JoinSelectResults3 { TableA_Id = 2, TableA_Bool = true, TableA_Name = "NameA2", Id = 2, TableBName = "NameB2" },
                new JoinSelectResults3 { TableA_Id = 2, TableA_Bool = true, TableA_Name = "NameA2", Id = 3, TableBName = "NameB3" },
            };
            Assert.That(rows3[0], Is.EqualTo(expected3[0]));
            Assert.That(rows3[1], Is.EqualTo(expected3[1]));
            Assert.That(rows3[2], Is.EqualTo(expected3[2]));
        }
        finally
        {
            db.GetLastSql().Print();
        }
    }
}