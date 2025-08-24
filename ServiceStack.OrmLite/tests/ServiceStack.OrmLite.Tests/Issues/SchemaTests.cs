using System;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Model;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Issues;

[TestFixtureOrmLite]
public class SchemaTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        if (!DialectFeatures.SchemaSupport) return;
        
        using var db = OpenDbConnection();
        db.CreateSchema<Editable>();
        db.CreateSchema<SchemaTable1>();
    }

    [Schema("Schema")]
    public class SchemaTable1
    {
        public int Id { get; set; }

        public int SchemaTable2Id { get; set; }

        [Reference]
        public SchemaTable2 Child { get; set; }
    }

    [Schema("Schema")]
    public class SchemaTable2
    {
        [AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; }
    }

    [Test]
    public void Can_join_on_table_with_schemas()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<SchemaTable1>();
        db.DropAndCreateTable<SchemaTable2>();

        db.Save(new SchemaTable1
        {
            Id = 1,
            Child = new SchemaTable2 { Name = "Foo" }
        }, references: true);

        db.Save(new SchemaTable1
        {
            Id = 2,
            Child = new SchemaTable2 { Name = "Bar" }
        }, references: true);

        var rows = db.Select(db.From<SchemaTable1>().Join<SchemaTable2>());
        Assert.That(rows.Count, Is.EqualTo(2));
        rows = db.Select(db.From<SchemaTable1>().Join<SchemaTable2>()
            .Where<SchemaTable2>(x => x.Name == "Foo"));
        Assert.That(rows.Count, Is.EqualTo(1));

        rows = db.Select(db.From<SchemaTable1>().LeftJoin<SchemaTable2>());
        Assert.That(rows.Count, Is.EqualTo(2));
        rows = db.Select(db.From<SchemaTable1>().LeftJoin<SchemaTable2>()
            .Where<SchemaTable2>(x => x.Name == "Foo"));
        Assert.That(rows.Count, Is.EqualTo(1));
    }

    [Test]
    public void Can_query_with_Schema_and_alias_attributes()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Section>();
        db.DropAndCreateTable<Page>();

        db.Save(new Page
        {
            SectionId = 1,
        }, references: true);
        db.Save(new Page
        {
            SectionId = 2,
        }, references: true);
        db.Save(new Section
        {
            Id = 1,
            Name = "Name1",
            ReportId = 15,
        }, references: true);

        var query = db.From<Section>()
            .LeftJoin<Section, Page>((s, p) => s.Id == p.SectionId)
            .Where<Section>(s => s.ReportId == 15);

        var results = db.Select(query);
        // results.PrintDump();

        Assert.That(results.Count, Is.EqualTo(1));
        Assert.That(results[0].Name, Is.EqualTo("Name1"));
    }

    [Test]
    public void Does_complex_query_using_Schemas_with_LeftJoins()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Editable>();
        db.DropAndCreateTable<EditableRevision>();
        db.DropAndCreateTable<LogEntry>();
        db.DropAndCreateTable<Report>();
        db.DropAndCreateTable<Page>();
        db.DropAndCreateTable<Section>();

        db.Insert(new Editable { Index = 1, Content = "Content", PageId = 1, Styles = "Styles", TypeId = 1 });
        db.Insert(new EditableRevision { Content = "Content", Styles = "Styles", Date = DateTime.UtcNow, EditableId = 1, EmployeeId = 1, Reason = "Reason" });
        db.Insert(new LogEntry { Date = DateTime.UtcNow, KlasId = 1, PageId = 1, PageTrackerId = 1, ReportId = 1, RequestUrl = "http://url.com", TypeId = 1 });
        db.Insert(new Report { DefaultAccessLevel = 1, Description = "Description", Name = "Name" });
        db.Insert(new Page { AccessLevel = 1, AssignedEmployeeId = 1, Index = 1, SectionId = 1, Template = "Template" });
        db.Insert(new Section { Name = "Name", ReportId = 1 });

        var q = db.From<Section>()
            .Join<Section, Page>((s, p) => s.Id == p.SectionId)
            .Join<Page, Editable>((p, e) => p.Id == e.PageId)
            .Where<Section, Page>((s, p) => s.ReportId == 1 && p.Index == 1);

        var result = db.Select<Editable>(q);
        result.PrintDump();

        db.GetLastSql().Print();
    }
}

[Alias("Editables")]
[Schema("Schema")]
public partial class Editable : IHasId<int>
{
    public string Content { get; set; }

    [Alias("EditableID")]
    [AutoIncrement]
    [PrimaryKey]
    public int Id { get; set; }

    [Required]
    public int Index { get; set; }

    [Alias("ReportPageID")]
    [Required]
    public int PageId { get; set; }

    public string Styles { get; set; }

    [Alias("Type")]
    [Required]
    public int TypeId { get; set; }
}

[Alias("EditableRevisions")]
[Schema("Schema")]
public partial class EditableRevision : IHasId<int>
{
    public string Content { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Alias("EditableID")]
    [Required]
    public int EditableId { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Alias("EditableRevisionsID")]
    [AutoIncrement]
    [PrimaryKey]
    public int Id { get; set; }

    public string Reason { get; set; }
    public string Styles { get; set; }
}

[Alias("LogEntries")]
[Schema("Schema")]
public class LogEntry : IHasId<int>
{
    [Required]
    public DateTime Date { get; set; }

    [Alias("LogEntriesID")]
    [AutoIncrement]
    [PrimaryKey]
    public int Id { get; set; }

    [Required]
    public int KlasId { get; set; }

    [Required]
    public int PageTrackerId { get; set; }

    [Alias("ReportID")]
    [Required]
    public int ReportId { get; set; }

    [Alias("ReportPageID")]
    [Required]
    public int PageId { get; set; }

    public string RequestUrl { get; set; }

    [Alias("Type")]
    [Required]
    public int TypeId { get; set; }
}

[Alias("ReportPages")]
[Schema("Schema")]
public partial class Page : IHasId<int>
{
    [Required]
    public int AccessLevel { get; set; }

    [Required]
    public int AssignedEmployeeId { get; set; }

    [Required]
    public bool Cover { get; set; }

    [Required]
    public bool Deleted { get; set; }

    [Required]
    public bool Disabled { get; set; }

    [Alias("ReportPageID")]
    [AutoIncrement]
    [PrimaryKey]
    public int Id { get; set; }

    [Required]
    public int Index { get; set; }

    public string Name { get; set; }

    [Alias("ReportSectionID")]
    [Required]
    public int SectionId { get; set; }

    public string Template { get; set; }
}

[Alias("Reports")]
[Schema("Schema")]
public partial class Report : IHasId<int>
{
    [Required]
    public int DefaultAccessLevel { get; set; }

    [Required]
    public bool Deleted { get; set; }

    public string Description { get; set; }

    [Alias("ReportID")]
    [AutoIncrement]
    [PrimaryKey]
    public int Id { get; set; }

    public string Name { get; set; }
}

[Alias("ReportSections")]
[Schema("Schema")]
public class Section : IHasId<int>
{
    [Alias("ReportSectionID")]
    [AutoIncrement]
    [PrimaryKey]
    public int Id { get; set; }

    public string Name { get; set; }

    [Alias("ReportID")]
    [Required]
    public int ReportId { get; set; }
}