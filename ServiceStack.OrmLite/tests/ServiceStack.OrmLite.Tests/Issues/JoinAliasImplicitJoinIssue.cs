using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.Issues;

[TestFixture]
public class JoinAliasImplicitJoinIssue : OrmLiteTestBase
{
    [Alias("DocumentProduction")]
    public class Document
    {
        [PrimaryKey]
        [Alias("DocumentProductionId")]
        public int Id { get; set; }

        [ForeignKey(typeof(DocumentTemplate))]
        [Alias("DocumentLinkId")]
        public int DocumentTemplateId { get; set; }
    }

    [Alias("Document")]
    public class DocumentTemplate
    {
        [AutoIncrement] 
        [Alias("DocumentId")] 
        public int? Id { get; set; }
        [Alias("Name")]
        public string Name { get; set; }
    }

    [OneTimeSetUp]
    public void Setup()
    {
        using var db = OpenDbConnection();
        db.DropTable<Document>();
        db.DropTable<DocumentTemplate>();

        db.CreateTable<DocumentTemplate>();
        db.CreateTable<Document>();
    }

    [Test]
    public void Can_join_tables_with_aliases()
    {
        using var db = OpenDbConnection();

        db.InsertAll([
            new DocumentTemplate
            {
                Id = 1,
                Name = nameof(DocumentTemplate) + "1",
            },
            new DocumentTemplate
            {
                Id = 2,
                Name = nameof(DocumentTemplate) + "2",
            },
        ]);

        var doc = new Document {
            Id = 1, 
            DocumentTemplateId = 2,
        };
        db.Insert(doc);

        List<DocumentTemplate> results;
        var q = db.From<DocumentTemplate>().Join<Document>();
        results = db.Select(q);
        Assert.That(results.Count, Is.EqualTo(1));
        Assert.That(results[0].Id, Is.EqualTo(2));
        Assert.That(results[0].Name, Is.EqualTo(nameof(DocumentTemplate) + "2"));
        
        var qInverse = db.From<Document>()
            .Join<DocumentTemplate>()
            .Select<DocumentTemplate>(x => x);

        results = db.Select<DocumentTemplate>(qInverse);
        Assert.That(results.Count, Is.EqualTo(1));
        Assert.That(results[0].Id, Is.EqualTo(2));
        Assert.That(results[0].Name, Is.EqualTo(nameof(DocumentTemplate) + "2"));
    }
}