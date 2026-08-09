using System;
using System.IO;
using NUnit.Framework;
using ServiceStack.AI;

namespace ServiceStack.Extensions.Tests;

[TestFixture]
public class PdfExtensionFileTests
{
    string root = null!;

    [SetUp]
    public void SetUp()
    {
        root = Path.Combine(Path.GetTempPath(), "pdf-files-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "lib"));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }

    [Test]
    public void Finds_direct_and_transitive_library_dependants_but_ignores_comments()
    {
        File.WriteAllText(Path.Combine(root, "lib", "v1.typ"), "#let theme(body) = body");
        File.WriteAllText(Path.Combine(root, "shared.typ"), "#import \"lib/v1.typ\": *");
        File.WriteAllText(Path.Combine(root, "invoice.typ"), "#import \"shared.typ\": *");
        File.WriteAllText(Path.Combine(root, "comment.typ"), "// #import \"lib/v1.typ\": *\n/* #include \"lib/v1.typ\" */");

        Assert.That(PdfExtension.FindDependants(root, "lib/v1.typ"), Is.EqualTo(new[]
        {
            "invoice.typ",
            "shared.typ",
        }));
    }

    [Test]
    public void Recognizes_versioned_libraries_but_not_preview_companions()
    {
        File.WriteAllText(Path.Combine(root, "lib", "v1.typ"), "");
        File.WriteAllText(Path.Combine(root, "lib", "v1.preview.typ"), "");
        File.WriteAllText(Path.Combine(root, "invoice.typ"), "");

        Assert.That(PdfExtension.IsLibraryTemplate("lib/v1.typ"), Is.True);
        Assert.That(PdfExtension.IsLibraryTemplate("lib/v1.preview.typ"), Is.False);
        Assert.That(PdfExtension.LibraryTemplates(root), Is.EqualTo(new[] { "lib/v1.typ" }));
    }
}
