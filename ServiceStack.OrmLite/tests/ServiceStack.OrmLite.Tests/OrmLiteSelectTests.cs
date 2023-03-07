using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Tests.Expression;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests;

[TestFixtureOrmLite]
public class OrmLiteSelectTests : OrmLiteProvidersTestBase
{
    public OrmLiteSelectTests(DialectContext context) : base(context) { }

    [Test]
    public void Can_GetById_int_from_ModelWithFieldsOfDifferentTypes_table()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<ModelWithFieldsOfDifferentTypes>();

        var rowIds = new List<int>(new[] { 1, 2, 3 });

        for (var i = 0; i < rowIds.Count; i++)
            rowIds[i] = (int)db.Insert(ModelWithFieldsOfDifferentTypes.Create(rowIds[i]), selectIdentity: true);

        var row = db.SingleById<ModelWithFieldsOfDifferentTypes>(rowIds[1]);

        Assert.That(row.Id, Is.EqualTo(rowIds[1]));
    }

    [Test]
    public void Can_GetById_string_from_ModelWithOnlyStringFields_table()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<ModelWithOnlyStringFields>();

        var rowIds = new List<string>(new[] { "id-1", "id-2", "id-3" });

        rowIds.ForEach(x => db.Insert(ModelWithOnlyStringFields.Create(x)));

        var row = db.SingleById<ModelWithOnlyStringFields>("id-1");

        Assert.That(row.Id, Is.EqualTo("id-1"));
    }

    [Test]
    public void Can_GetByIds_int_from_ModelWithFieldsOfDifferentTypes_table()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<ModelWithFieldsOfDifferentTypes>();

        var rowIds = new List<int>(new[] { 1, 2, 3 });

        for (var i = 0; i < rowIds.Count; i++)
            rowIds[i] = (int)db.Insert(ModelWithFieldsOfDifferentTypes.Create(rowIds[i]), selectIdentity: true);

        var rows = db.SelectByIds<ModelWithFieldsOfDifferentTypes>(rowIds);
        var dbRowIds = rows.ConvertAll(x => x.Id);

        Assert.That(dbRowIds, Is.EquivalentTo(rowIds));
    }

    [Test]
    public void Can_GetByIds_string_from_ModelWithOnlyStringFields_table()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<ModelWithOnlyStringFields>();

        var rowIds = new List<string>(new[] { "id-1", "id-2", "id-3" });

        rowIds.ForEach(x => db.Insert(ModelWithOnlyStringFields.Create(x)));

        var rows = db.SelectByIds<ModelWithOnlyStringFields>(rowIds);
        var dbRowIds = rows.ConvertAll(x => x.Id);

        Assert.That(dbRowIds, Is.EquivalentTo(rowIds));
    }

    [Test]
    public void Can_select_with_filter_from_ModelWithOnlyStringFields_table()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<ModelWithOnlyStringFields>();

        var rowIds = new List<string>(new[] { "id-1", "id-2", "id-3" });

        rowIds.ForEach(x => db.Insert(ModelWithOnlyStringFields.Create(x)));

        var filterRow = ModelWithOnlyStringFields.Create("id-4");
        filterRow.AlbumName = "FilteredName";

        db.Insert(filterRow);

        var rows = db.Select<ModelWithOnlyStringFields>("AlbumName".SqlColumn(DialectProvider) + " = @album".PreNormalizeSql(db), new { album = filterRow.AlbumName });
        var dbRowIds = rows.ConvertAll(x => x.Id);

        Assert.That(dbRowIds, Has.Count.EqualTo(1));
        Assert.That(dbRowIds[0], Is.EqualTo(filterRow.Id));
    }

    [Test]
    public void Can_select_scalar_value()
    {
        const int n = 5;

        using var db = OpenDbConnection();
        db.DropAndCreateTable<ModelWithIdAndName>();

        n.Times(x => db.Insert(ModelWithIdAndName.Create(x)));

        var count = db.Scalar<int>("SELECT COUNT(*) FROM " + "ModelWithIdAndName".SqlTable(DialectProvider));

        Assert.That(count, Is.EqualTo(n));
    }

    [Test]
    public void Can_loop_each_string_from_ModelWithOnlyStringFields_table()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<ModelWithOnlyStringFields>();

        var rowIds = new List<string>(new[] { "id-1", "id-2", "id-3" });

        rowIds.ForEach(x => db.Insert(ModelWithOnlyStringFields.Create(x)));

        var dbRowIds = new List<string>();
        foreach (var row in db.SelectLazy<ModelWithOnlyStringFields>())
        {
            dbRowIds.Add(row.Id);
        }

        Assert.That(dbRowIds, Is.EquivalentTo(rowIds));
    }

    [Test]
    public void Can_loop_each_string_from_ModelWithOnlyStringFields_table_Column()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<ModelWithOnlyStringFields>();

        var rowIds = new List<string>(new[] { "id-1", "id-2", "id-3" });

        rowIds.ForEach(x => db.Insert(ModelWithOnlyStringFields.Create(x)));

        var dbRowIds = new List<string>();
        foreach (var rowId in db.ColumnLazy<string>(
                     db.From<ModelWithOnlyStringFields>().Select(x => x.Id)))
        {
            dbRowIds.Add(rowId);
        }

        Assert.That(dbRowIds, Is.EquivalentTo(rowIds));
    }

    [Test]
    public void Can_loop_each_with_filter_from_ModelWithOnlyStringFields_table()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<ModelWithOnlyStringFields>();

        var rowIds = new List<string>(new[] { "id-1", "id-2", "id-3" });

        rowIds.ForEach(x => db.Insert(ModelWithOnlyStringFields.Create(x)));

        var filterRow = ModelWithOnlyStringFields.Create("id-4");
        filterRow.AlbumName = "FilteredName";

        db.Insert(filterRow);

        var dbRowIds = new List<string>();
        var rows = db.SelectLazy<ModelWithOnlyStringFields>("AlbumName".SqlColumn(DialectProvider) + " = @AlbumName".PreNormalizeSql(db), new { filterRow.AlbumName });
        foreach (var row in rows)
        {
            dbRowIds.Add(row.Id);
        }

        Assert.That(dbRowIds, Has.Count.EqualTo(1));
        Assert.That(dbRowIds[0], Is.EqualTo(filterRow.Id));
    }

    [Test]
    public void Can_GetFirstColumn()
    {
        const int n = 5;

        using var db = OpenDbConnection();
        db.DropAndCreateTable<ModelWithIdAndName>();

        n.Times(x => db.Insert(ModelWithIdAndName.Create(x)));

        var ids = db.Column<int>("SELECT Id FROM " + "ModelWithIdAndName".SqlTable(DialectProvider));

        Assert.That(ids.Count, Is.EqualTo(n));
    }

    [Test]
    public void Can_GetFirstColumnDistinct()
    {
        const int n = 5;

        using var db = OpenDbConnection();
        db.DropAndCreateTable<ModelWithIdAndName>();

        n.Times(x => db.Insert(ModelWithIdAndName.Create(x)));

        var ids = db.ColumnDistinct<int>("SELECT Id FROM " + "ModelWithIdAndName".SqlTable(DialectProvider));

        Assert.That(ids.Count, Is.EqualTo(n));
    }

    [Test]
    public void Can_GetLookup()
    {
        const int n = 5;

        using var db = OpenDbConnection();
        db.DropAndCreateTable<ModelWithIdAndName>();

        n.Times(x =>
        {
            var row = ModelWithIdAndName.Create(x);
            row.Name = x % 2 == 0 ? "OddGroup" : "EvenGroup";
            db.Insert(row);
        });

        var lookup = db.Lookup<string, int>("SELECT Name, Id FROM " + "ModelWithIdAndName".SqlTable(DialectProvider));

        Assert.That(lookup, Has.Count.EqualTo(2));
        Assert.That(lookup["OddGroup"], Has.Count.EqualTo(3));
        Assert.That(lookup["EvenGroup"], Has.Count.EqualTo(2));
    }

    [Test]
    public void Can_GetDictionary()
    {
        const int n = 5;

        using var db = OpenDbConnection();
        db.DropAndCreateTable<ModelWithIdAndName>();

        n.Times(x => db.Insert(ModelWithIdAndName.Create(x)));

        var dictionary = db.Dictionary<int, string>("SELECT Id, Name FROM {0}".Fmt("ModelWithIdAndName".SqlTable(DialectProvider)));

        Assert.That(dictionary, Has.Count.EqualTo(5));

        //Console.Write(dictionary.Dump());
    }

    [Test]
    [IgnoreDialect(Tests.Dialect.AnyOracle, "Oracle provider doesn't modify user supplied SQL to conform to name length restrictions")]
    public void Can_Select_subset_ModelWithIdAndName_from_ModelWithFieldsOfDifferentTypes_table()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<ModelWithFieldsOfDifferentTypes>();

        var rowIds = new List<int>(new[] { 1, 2, 3 });

        for (var i = 0; i < rowIds.Count; i++)
            rowIds[i] = (int)db.Insert(ModelWithFieldsOfDifferentTypes.Create(rowIds[i]), selectIdentity: true);

        var rows = db.Select<ModelWithIdAndName>("SELECT Id, Name FROM " + "ModelWithFieldsOfDifferentTypes".SqlTable(DialectProvider));
        var dbRowIds = rows.ConvertAll(x => x.Id);

        Assert.That(dbRowIds, Is.EquivalentTo(rowIds));
    }

    [Test]
    public void Can_Select_Into_ModelWithIdAndName_from_ModelWithFieldsOfDifferentTypes_table()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<ModelWithFieldsOfDifferentTypes>();

        var rowIds = new List<int>(new[] { 1, 2, 3 });

        for (var i = 0; i < rowIds.Count; i++)
            rowIds[i] = (int)db.Insert(ModelWithFieldsOfDifferentTypes.Create(rowIds[i]), selectIdentity: true);

        var rows = db.Select<ModelWithIdAndName>(typeof(ModelWithFieldsOfDifferentTypes));
        var dbRowIds = rows.ConvertAll(x => x.Id);

        Assert.That(dbRowIds, Is.EquivalentTo(rowIds));
    }


    [Test]
    public void Can_Select_In_for_string_value()
    {
        const int n = 5;

        using var db = OpenDbConnection();
        db.DropAndCreateTable<ModelWithIdAndName>();

        n.Times(x => db.Insert(ModelWithIdAndName.Create(x)));

        var selectInNames = new[] { "Name1", "Name2" };
        var rows = db.Select<ModelWithIdAndName>("Name IN ({0})".Fmt(selectInNames.SqlInParams(DialectProvider)),
            new { values = selectInNames.SqlInValues(DialectProvider) });
        Assert.That(rows.Count, Is.EqualTo(selectInNames.Length));

        rows = db.Select<ModelWithIdAndName>("Name IN (@values)",
            new { values = selectInNames });
        Assert.That(rows.Count, Is.EqualTo(selectInNames.Length));

        rows = db.Select<ModelWithIdAndName>("Name IN (@p1, @p2)".PreNormalizeSql(db), new { p1 = "Name1", p2 = "Name2" });
        Assert.That(rows.Count, Is.EqualTo(selectInNames.Length));
    }

    [Test]
    public void Can_select_IN_using_array_or_List_params()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<ModelWithIdAndName>();
        5.Times(x => db.Insert(ModelWithIdAndName.Create(x)));

        var names = new[] { "Name2", "Name3" };
        var rows = db.Select<ModelWithIdAndName>("Name IN (@names)", new { names });
        Assert.That(rows.Count, Is.EqualTo(2));
        Assert.That(rows.Map(x => x.Name), Is.EquivalentTo(names));

        var ids = new List<int> { 2, 3 };
        rows = db.Select<ModelWithIdAndName>("Id IN (@ids)", new { ids });
        Assert.That(rows.Count, Is.EqualTo(2));
        Assert.That(rows.Map(x => x.Id), Is.EquivalentTo(ids));
    }

    [Test]
    public void Can_use_array_param_for_ExecuteSql()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<ModelWithIdAndName>();
        5.Times(x => db.Insert(ModelWithIdAndName.Create(x + 1)));

        var q = db.From<ModelWithIdAndName>();

        db.ExecuteSql($"UPDATE {q.Table<ModelWithIdAndName>()} SET Name = 'updated' WHERE Id IN (@ids)",
            new { ids = new[] { 1, 2, 3 } });

        var count = db.Count<ModelWithIdAndName>(x => x.Name == "updated");
        Assert.That(count, Is.EqualTo(3));

        db.ExecuteSql($"UPDATE {q.Table<ModelWithIdAndName>()} SET Name = 'updated' WHERE Name IN (@names)",
            new { names = new[] { "Name4", "Name5" } });

        count = db.Count<ModelWithIdAndName>(x => x.Name == "updated");
        Assert.That(count, Is.EqualTo(5));
    }

    public class PocoFlag
    {
        public string Name { get; set; }
        public bool Flag { get; set; }
    }

    [Test]
    public void Can_populate_PocoFlag()
    {
        using var db = OpenDbConnection();
        var fromDual = "";
        if (Dialect == Dialect.Firebird)
            fromDual = " FROM RDB$DATABASE";

        var rows = db.Select<PocoFlag>("SELECT 1 as Flag" + fromDual);
        Assert.That(rows[0].Flag);
    }

    public class PocoFlagWithId
    {
        public int Id { get; set; }
        public bool Flag { get; set; }
    }

    [Test]
    public void Can_populate_PocoFlagWithId()
    {
        using var db = OpenDbConnection();
        var fromDual = "";
        if (Dialect == Dialect.Firebird)
            fromDual = " FROM RDB$DATABASE";

        var rows = db.Select<PocoFlagWithId>("SELECT 1 as Id, 1 as Flag" + fromDual);

        Assert.That(rows[0].Id, Is.EqualTo(1));
        Assert.That(rows[0].Flag);
    }

    public class TypeWithTimeSpan
    {
        public int Id { get; set; }
        public TimeSpan TimeSpan { get; set; }
    }

    [Test]
    public void Can_handle_TimeSpans()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<TypeWithTimeSpan>();

        var timeSpan = new TimeSpan(1, 1, 1, 1);
        db.Insert(new TypeWithTimeSpan { Id = 1, TimeSpan = timeSpan });

        var model = db.SingleById<TypeWithTimeSpan>(1);

        Assert.That(model.TimeSpan, Is.EqualTo(timeSpan));
    }

    [Test]
    public void Does_return_correct_numeric_values()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<ModelWithDifferentNumTypes>();

        var row = ModelWithDifferentNumTypes.Create(1);

        db.Insert(row);

        var fromDb = db.Select<ModelWithDifferentNumTypes>().First();

        Assert.That(row.Short, Is.EqualTo(fromDb.Short));
        Assert.That(row.Int, Is.EqualTo(fromDb.Int));
        Assert.That(row.Long, Is.EqualTo(fromDb.Long));
        Assert.That(row.Float, Is.EqualTo(fromDb.Float));
        Assert.That(row.Double, Is.EqualTo(fromDb.Double).Within(1d));
        Assert.That(row.Decimal, Is.EqualTo(fromDb.Decimal));
    }

    [Test]
    public void Does_not_evaluate_SqlFmt_when_no_params()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<ModelWithIdAndName>();

        db.Insert(new ModelWithIdAndName(1) { Name = "{test}" });

        var rows = db.Select<ModelWithIdAndName>("Name = '{test}'");

        Assert.That(rows.Count, Is.EqualTo(1));
    }
        
    public class AccountIntId
    {
        [AutoIncrement]
        public int Id { get; set; }

        [Required]
        [Unique]
        public string Username { get; set; }
    }

    [Test]
    public void Does_return_null_when_no_record_with_id_exists_in_AccountIntId()
    {
        using var db = OpenDbConnection();
        OrmLiteUtils.PrintSql();
        db.DropAndCreateTable<AccountIntId>();
        db.Insert(new AccountIntId { Username = "johnsmith" });
        db.Insert(new AccountIntId { Username = "2-whatever-more-3" });
            
        Assert.That(db.SingleById<AccountIntId>(1).Username, Is.EqualTo("johnsmith"));
        Assert.That(db.SingleById<AccountIntId>(3), Is.Null);

        AccountIntId result = null;
        try
        {
            result = db.SingleById<AccountIntId>("");
        }
        catch {}
        Assert.That(result, Is.Null);

        try
        {
            result = db.SingleById<AccountIntId>("johnsmith2");
        }
        catch {}
        Assert.That(result, Is.Null);
        
        Assert.Throws<ArgumentNullException>(() => db.SingleById<AccountIntId>(null));

        // Returns Id=2 in Sqlite/MySql
        // Assert.That(db.SingleById<AccountIntId>("2-whatever-more-3"), Is.Null);
    }

    [TestCase(1E125)]
    [TestCase(-1E125)]
    public void Does_return_large_double_values(double value)
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<ModelWithDifferentNumTypes>();
        var expected = new ModelWithDifferentNumTypes { Double = value };

        var id = db.Insert(expected, true);
        var actual = db.SingleById<ModelWithDifferentNumTypes>(id);

        Assert.That(expected.Double, Is.EqualTo(actual.Double).
            Or.EqualTo(-9.9999999999999992E+124d).
            Or.EqualTo(9.9999999999999992E+124d)); //Firebird
    }

    public class CustomSql
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int CustomMax { get; set; }
        public int CustomCount { get; set; }
    }
    
    [Test]
    [IgnoreDialect(Dialect.MySql, "Does not support LIKE escape sequences")]
    public void Does_support_LIKE_Escape_Char()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<CustomSql>();
        db.Insert(new CustomSql { Id = 1, Name = "Jo[h]n" });
        
        var results = db.Select<CustomSql>("name LIKE @name", new { name = "Jo\\[h\\]n" });
        if (!Dialect.AnyPostgreSql.HasFlag(Dialect))
        {
            Assert.That(results.Count, Is.EqualTo(0));
        }
        results = db.Select<CustomSql>("name LIKE @name ESCAPE '\\'", new { name = "Jo\\[h\\]n" });
        Assert.That(results.Count, Is.EqualTo(1));
    }

    [Test]
    public void Does_project_Sql_columns()
    {
        OrmLiteConfig.BeforeExecFilter = cmd => cmd.GetDebugString().Print();
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Rockstar>();
        db.DropAndCreateTable<RockstarAlbum>();
        db.Insert(AutoQueryTests.SeedRockstars);
        db.Insert(AutoQueryTests.SeedAlbums);

        var q = db.From<Rockstar>()
            .Join<RockstarAlbum>()
            .GroupBy(r => new { r.Id, r.FirstName, r.LastName })
            .Select<Rockstar, RockstarAlbum>((r, a) => new
            {
                r.Id,
                Name = r.FirstName + " " + r.LastName,
                CustomMax = Sql.Max(r.Id),
                CustomCount = Sql.Count(r.Id > a.Id ? r.Id + 2 : a.Id + 2)
            });

        var results = db.Select<CustomSql>(q);
        var result = results[0];

        //                results.PrintDump();

        Assert.That(result.Id, Is.GreaterThan(0));
        Assert.That(result.Name, Is.Not.Null);
        Assert.That(result.CustomMax, Is.GreaterThan(0));
        Assert.That(result.CustomCount, Is.GreaterThan(0));
    }

    [Test]
    public void Can_select_from_Tasked_with_single_tags()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Tasked>();

        var parentId = db.Insert(new Tasked { Created = new DateTime(2000, 01, 01) }, selectIdentity: true);
        db.Insert(new Tasked { ParentId = parentId, Created = new DateTime(2001, 01, 01) }, selectIdentity: true);
        var tag = "Query Tasked";
        var q = db.From<Tasked>().TagWith(tag);

        Debug.Assert(q.Tags.Count == 1);
        Debug.Assert(q.Tags.ToList()[0]== tag);

        var select = q.ToSelectStatement();
        Debug.Assert(select.Contains(tag));

        var results = db.Select(q);
        Debug.Assert(results.Count == 2);
    }

    [Test]
    public void Can_select_from_Tasked_with_multi_tags()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Tasked>();

        var parentId = db.Insert(new Tasked { Created = new DateTime(2000, 01, 01) }, selectIdentity: true);
        db.Insert(new Tasked { ParentId = parentId, Created = new DateTime(2001, 01, 01) }, selectIdentity: true);
        var tag1 = "Query Tasked 1";
        var tag2 = "Query Tasked 2";
        var q = db.From<Tasked>()
            .TagWith(tag1)
            .TagWith(tag2);

        Debug.Assert(q.Tags.Count == 2);
        Debug.Assert(q.Tags.ToList()[0] == tag1);
        Debug.Assert(q.Tags.ToList()[1] == tag2);

        var select = q.ToSelectStatement();
        Debug.Assert(select.Contains(tag1));
        Debug.Assert(select.Contains(tag2));

        var results = db.Select(q);
        Debug.Assert(results.Count == 2);
    }

    [Test]
    public void Can_select_from_Tasked_with_callsite_tags()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Tasked>();

        var parentId = db.Insert(new Tasked { Created = new DateTime(2000, 01, 01) }, selectIdentity: true);
        db.Insert(new Tasked { ParentId = parentId, Created = new DateTime(2001, 01, 01) }, selectIdentity: true);

        var q = db.From<Tasked>().TagWithCallSite(nameof(OrmLiteSelectTests), 13);
        var tag = $"File: {nameof(OrmLiteSelectTests)}:13";
        Debug.Assert(q.Tags.Count == 1);
        Debug.Assert(q.Tags.ToList()[0] == tag);

        var select = q.ToSelectStatement();
        Debug.Assert(select.Contains(tag));

        var results = db.Select(q);
        Debug.Assert(results.Count == 2);
    }
}