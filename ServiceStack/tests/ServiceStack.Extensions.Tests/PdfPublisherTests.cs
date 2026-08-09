using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using ServiceStack.AI;
using ServiceStack.Text;

namespace ServiceStack.Extensions.Tests;

[TestFixture]
public class PdfPublisherTests
{
    string root = null!;
    PdfPublisher publisher = null!;

    [SetUp]
    public void SetUp()
    {
        root = Path.Combine(Path.GetTempPath(), "pdf-versions-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        publisher = new PdfPublisher(new PdfFeature { PdfPath = root });
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }

    [Test]
    public void Can_save_and_rollback_immutable_filesystem_revisions()
    {
        Write("invoice.typ", "version one");
        Write("invoice.json", "{\"version\":1}");
        var first = publisher.SaveRevision("invoice", "author", "invoices/invoice.typ",
            ["invoice.typ", "invoice.json"]);

        Write("invoice.typ", "version two");
        Write("invoice.json", "{\"version\":2}");
        var second = publisher.SaveRevision("invoice", "author", "invoices/invoice.typ",
            ["invoice.typ", "invoice.json"]);

        var result = publisher.Rollback("invoice", first.Id, "admin");

        Assert.That(File.ReadAllText(Path.Combine(root, "invoice.typ")), Is.EqualTo("version one"));
        Assert.That(File.ReadAllText(Path.Combine(root, "invoice.json")), Is.EqualTo("{\"version\":1}"));
        Assert.That(result.Revision.Action, Is.EqualTo("rollback"));
        Assert.That(result.Revision.RestoredFrom, Is.EqualTo(first.Id));
        Assert.That(result.Revision.Id, Is.Not.EqualTo(first.Id));
        Assert.That(publisher.GetManifest().GetObject("invoice").GetString("currentRevision"),
            Is.EqualTo(result.Revision.Id));

        var revisions = publisher.GetRevisions("invoice");
        Assert.That(revisions.Select(x => x.Id), Does.Contain(first.Id));
        Assert.That(revisions.Select(x => x.Id), Does.Contain(second.Id));
        Assert.That(revisions.Select(x => x.Id), Does.Contain(result.Revision.Id));
    }

    [Test]
    public void Rollback_removes_files_added_by_newer_revision()
    {
        Write("invoice.typ", "version one");
        var first = publisher.SaveRevision("invoice", "author", "invoice.typ", ["invoice.typ"]);

        Write("invoice.typ", "version two");
        Write("invoice.logo.png", "new asset");
        publisher.SaveRevision("invoice", "author", "invoice.typ", ["invoice.typ", "invoice.logo.png"]);

        publisher.Rollback("invoice", first.Id, "admin");

        Assert.That(File.Exists(Path.Combine(root, "invoice.logo.png")), Is.False);
    }

    [Test]
    public void Publishing_captures_a_scoped_library_in_the_documents_flat_file_set()
    {
        var source = Path.Combine(root, "source");
        Directory.CreateDirectory(Path.Combine(source, "lib"));
        File.WriteAllText(Path.Combine(source, "invoice.typ"),
            "#import \"lib/v1.typ\": *\n#let data = load-data(\"../invoice.json\")");
        File.WriteAllText(Path.Combine(source, "invoice.json"), "{}");
        File.WriteAllText(Path.Combine(source, "lib", "v1.typ"),
            "#let load-data(fallback) = json(fallback)");

        var result = publisher.Publish(source, "invoice.typ", "invoice", "author");

        Assert.That(result.Files, Does.Contain("invoice.v1.typ"));
        Assert.That(File.ReadAllText(Path.Combine(root, "invoice.typ")),
            Does.Contain("#import \"invoice.v1.typ\""));
        Assert.That(File.ReadAllText(Path.Combine(root, "invoice.typ")),
            Does.Contain("load-data(\"invoice.json\")"));
    }

    void Write(string fileName, string contents) => File.WriteAllText(Path.Combine(root, fileName), contents);
}
