#nullable enable
using System;
using System.Linq;
using NUnit.Framework;
using ServiceStack.AI;

namespace ServiceStack.Extensions.Tests;

public class AiChatCalculatorTests
{
    [TestCase("1+1", "2")]
    [TestCase("2 * 3 + 4", "10")]
    [TestCase("2 * (3 + 4)", "14")]
    [TestCase("10 / 4", "2.5")]
    [TestCase("10 % 3", "1")]
    [TestCase("2 ** 10", "1024")]
    [TestCase("2 ^ 10", "1024")]
    [TestCase("-5 + 3", "-2")]
    [TestCase("17 * 23", "391")]
    public void Evaluates_arithmetic(string expression, string expected) =>
        Assert.That(Calculator.Evaluate(expression), Is.EqualTo(expected));

    [TestCase("1 < 2", "True")]
    [TestCase("2 <= 2", "True")]
    [TestCase("3 > 4", "False")]
    [TestCase("3 >= 4", "False")]
    [TestCase("2 == 2", "True")]
    [TestCase("2 != 2", "False")]
    [TestCase("1 < 2 and 3 > 2", "True")]
    [TestCase("1 > 2 or 3 > 2", "True")]
    [TestCase("not 1 > 2", "True")]
    public void Evaluates_comparisons_and_booleans(string expression, string expected) =>
        Assert.That(Calculator.Evaluate(expression), Is.EqualTo(expected));

    [TestCase("sqrt(16)", "4")]
    [TestCase("abs(-7)", "7")]
    [TestCase("max(1, 5, 3)", "5")]
    [TestCase("min(1, 5, 3)", "1")]
    [TestCase("sum(1, 2, 3)", "6")]
    [TestCase("round(3.7)", "4")]
    [TestCase("mean(2, 4, 6)", "4")]
    [TestCase("median(1, 3, 2)", "2")]
    [TestCase("floor(3.7)", "3")]
    [TestCase("ceil(3.2)", "4")]
    [TestCase("factorial(5)", "120")]
    [TestCase("gcd(12, 18)", "6")]
    public void Evaluates_functions(string expression, string expected) =>
        Assert.That(Calculator.Evaluate(expression), Is.EqualTo(expected));

    [Test]
    public void Evaluates_constants()
    {
        Assert.That(Calculator.Evaluate("pi"), Does.StartWith("3.14159"));
        Assert.That(Calculator.Evaluate("e"), Does.StartWith("2.71828"));
        Assert.That(Calculator.Evaluate("inf"), Is.EqualTo("inf"));
    }

    [Test]
    public void Rejects_arbitrary_code_and_unknown_identifiers()
    {
        // the whole point of the AST-style evaluator: no code execution
        Assert.Throws<ArgumentException>(() => Calculator.Evaluate("__import__('os').system('ls')"));
        Assert.Throws<ArgumentException>(() => Calculator.Evaluate("open('/etc/passwd')"));
        Assert.Throws<ArgumentException>(() => Calculator.Evaluate("foo"));
        Assert.Throws<ArgumentException>(() => Calculator.Evaluate("1 + ; DROP TABLE"));
    }

    [Test]
    public void Exposes_functions_and_constants_for_the_ui()
    {
        Assert.That(Calculator.Constants, Does.Contain("pi"));
        Assert.That(Calculator.FunctionNames, Does.Contain("sqrt"));
        // constants aren't listed as functions (Python filters them out)
        Assert.That(Calculator.FunctionNames.Intersect(Calculator.Constants), Is.Empty);
    }
}

public class ChatMediaTests
{
    [Test]
    public void Picks_the_closest_configured_aspect_ratio()
    {
        Assert.That(ChatDb.ClosestAspectRatio(1024, 1024), Is.EqualTo("1:1"));
        Assert.That(ChatDb.ClosestAspectRatio(1344, 768), Is.EqualTo("16:9"));
        Assert.That(ChatDb.ClosestAspectRatio(768, 1344), Is.EqualTo("9:16"));
        Assert.That(ChatDb.ClosestAspectRatio(1248, 832), Is.EqualTo("3:2"));
        // near-misses snap to the nearest configured ratio
        Assert.That(ChatDb.ClosestAspectRatio(1920, 1080), Is.EqualTo("16:9"));
    }

    [Test]
    public void Groups_ratios_into_format_buckets()
    {
        Assert.That(ChatDb.MediaFormats["square"], Does.Contain("1:1"));
        Assert.That(ChatDb.MediaFormats["landscape"], Does.Contain("16:9"));
        Assert.That(ChatDb.MediaFormats["portrait"], Does.Contain("9:16"));
        Assert.That(ChatDb.MediaFormats["landscape"], Does.Not.Contain("9:16"));
    }

    [Test]
    public void Media_dto_exposes_snake_case_aspect_ratio()
    {
        var media = new ChatMedia
        {
            Id = 1,
            Type = "image",
            Url = "/~cache/ab/abc.png",
            Hash = "abc",
            AspectRatio = "16:9",
            Created = new DateTime(2026, 7, 24, 10, 0, 0),
        };
        var dto = media.ToDto();
        // the gallery UI reads aspect_ratio, matching Python's column name
        Assert.That(dto["aspect_ratio"]!.GetValue<string>(), Is.EqualTo("16:9"));
        Assert.That(dto["type"]!.GetValue<string>(), Is.EqualTo("image"));
    }
}
