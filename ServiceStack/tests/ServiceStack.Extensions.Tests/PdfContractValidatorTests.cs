using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using ServiceStack.AI;

namespace ServiceStack.Extensions.Tests;

[TestFixture]
public class PdfContractValidatorTests
{
    string root = null!;
    PdfContractValidator validator = null!;

    [SetUp]
    public void SetUp()
    {
        root = Path.Combine(Path.GetTempPath(), "pdf-contract-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        validator = new PdfContractValidator(new PdfFeature { PdfPath = root });
        Write("invoice.typ", "#let value = data.customer.name\n#data.unknownField");
        Write("invoice.ui.json", Schema);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }

    [Test]
    public void Validates_example_and_named_fixtures()
    {
        Write("invoice.json", """{"customer":{"name":"Alice"},"amount":12.50,"issued":"2026-08-10"}""");
        Write("invoice.fixture.empty.json", """{"customer":{"name":"None"},"amount":0,"issued":"2026-01-01"}""");

        var result = validator.Validate(root, "invoice.typ", "invoice");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Fixtures, Is.EqualTo(new[] { "empty" }));
        Assert.That(result.Issues.Any(x => x.Code == "UnknownDataPath" && x.Severity == "warning"), Is.True);
    }

    [Test]
    public void Rejects_contract_type_required_bound_and_format_errors()
    {
        Write("invoice.json", """{"customer":{},"amount":-1,"issued":"10/08/2026"}""");
        Write("invoice.fixture.bad.json", """{"customer":{"name":3},"amount":2,"issued":"2026-08-10"}""");

        var result = validator.Validate(root, "invoice.typ", "invoice");

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Issues.Any(x => x.Code == "Required" && x.Fixture == "example"), Is.True);
        Assert.That(result.Issues.Any(x => x.Code == "minimum" && x.Fixture == "example"), Is.True);
        Assert.That(result.Issues.Any(x => x.Code == "Format" && x.Fixture == "example"), Is.True);
        Assert.That(result.Issues.Any(x => x.Code == "Type" && x.Fixture == "bad"), Is.True);
    }

    [Test]
    public void Invalid_json_is_a_contract_error()
    {
        Write("invoice.json", "{");

        var result = validator.Validate(root, "invoice.typ", "invoice");

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Issues.Single(x => x.Code == "InvalidJson").Fixture, Is.EqualTo("example"));
    }

    void Write(string fileName, string contents) => File.WriteAllText(Path.Combine(root, fileName), contents);

    const string Schema = """
    {
      "type":"object",
      "additionalProperties":false,
      "required":["customer","amount","issued"],
      "properties":{
        "customer":{"type":"object","required":["name"],"properties":{"name":{"type":"string","minLength":1}}},
        "amount":{"type":"number","minimum":0,"multipleOf":0.01},
        "issued":{"type":"string","format":"date"}
      }
    }
    """;
}
