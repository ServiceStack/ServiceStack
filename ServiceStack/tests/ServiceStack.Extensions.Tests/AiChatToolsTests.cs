#nullable enable
using System;
using System.IO;
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

public class HtmlToMarkdownParserTests
{
    [Test]
    public void Converts_html_elements_to_markdown()
    {
        var html = """
            <!DOCTYPE html>
            <html>
            <head><title>Test Page</title><style>body { color: red; }</style></head>
            <body>
                <script>console.log('ignore');</script>
                <h1>Main Title</h1>
                <p>This is a paragraph with <b>bold text</b>, <i>italic text</i>, and a <a href="/docs/guide">Documentation Link</a>.</p>
                <pre><code>public void Hello()
                {
                    Console.WriteLine("Hello world");
                }</code></pre>
                <ul>
                    <li>Item 1</li>
                    <li>Item 2</li>
                </ul>
                <blockquote>A wise quote</blockquote>
                <table>
                    <tr><th>Name</th><th>Role</th></tr>
                    <tr><td>Alice</td><td>Admin</td></tr>
                </table>
            </body>
            </html>
            """;

        var parser = new HtmlToMarkdownParser("https://example.com/base/");
        var md = parser.Parse(html);

        Assert.That(md, Does.Contain("# Main Title"));
        Assert.That(md, Does.Contain("**bold text**"));
        Assert.That(md, Does.Contain("*italic text*"));
        Assert.That(md, Does.Contain("[Documentation Link](https://example.com/docs/guide)"));
        Assert.That(md, Does.Contain("```\npublic void Hello()"));
        Assert.That(md, Does.Contain("- Item 1"));
        Assert.That(md, Does.Contain("- Item 2"));
        Assert.That(md, Does.Contain("> A wise quote"));
        Assert.That(md, Does.Contain("| Name | Role |"));
        Assert.That(md, Does.Contain("| Alice | Admin |"));
        Assert.That(md, Does.Not.Contain("console.log"));
        Assert.That(md, Does.Not.Contain("color: red"));
    }

    [Test]
    public void Handles_empty_and_plain_text()
    {
        var parser = new HtmlToMarkdownParser();
        Assert.That(parser.Parse(""), Is.EqualTo(""));
        Assert.That(parser.Parse("Just plain text"), Is.EqualTo("Just plain text"));
    }
}

public class GrepSearchTests
{
    [Test]
    public void Grep_searches_files_and_directories()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "grep-test-" + Guid.NewGuid().ToString("n")[..8]);
        Directory.CreateDirectory(tempDir);
        try
        {
            var f1 = Path.Combine(tempDir, "Test1.cs");
            var f2 = Path.Combine(tempDir, "Test2.txt");
            var ignoredDir = Path.Combine(tempDir, "node_modules");
            Directory.CreateDirectory(ignoredDir);
            var f3 = Path.Combine(ignoredDir, "Ignored.cs");

            File.WriteAllText(f1, "public class CalculatorService\n{\n    public int Add(int a, int b) => a + b;\n}\n");
            File.WriteAllText(f2, "Note: CalculatorService should be tested.\nAnother line.\n");
            File.WriteAllText(f3, "CalculatorService in node_modules\n");

            var ext = new CoreToolsExtension();
            var feature = new ChatFeature();
            var ctx = new ExtensionContext(feature, "core_tools");
            ext.Install(ctx);

            var toolDef = ctx.GetToolDefinition("grep_search");
            Assert.That(toolDef, Is.Not.Null);

            var chatCtx = new ChatContext();

            // Literal search
            var res = (string)(ctx.Feature.Tools.GetTool("grep_search")!.Handler(new System.Text.Json.Nodes.JsonObject
            {
                ["query"] = "CalculatorService",
                ["path"] = tempDir,
            }, chatCtx).Result ?? "");

            Assert.That(res, Does.Contain("Test1.cs:1: public class CalculatorService"));
            Assert.That(res, Does.Contain("Test2.txt:1: Note: CalculatorService"));
            Assert.That(res, Does.Not.Contain("node_modules"));

            // File pattern filter
            var resPattern = (string)(ctx.Feature.Tools.GetTool("grep_search")!.Handler(new System.Text.Json.Nodes.JsonObject
            {
                ["query"] = "CalculatorService",
                ["path"] = tempDir,
                ["file_pattern"] = "*.cs",
            }, chatCtx).Result ?? "");

            Assert.That(resPattern, Does.Contain("Test1.cs:1:"));
            Assert.That(resPattern, Does.Not.Contain("Test2.txt"));

            // Regex search
            var resRegex = (string)(ctx.Feature.Tools.GetTool("grep_search")!.Handler(new System.Text.Json.Nodes.JsonObject
            {
                ["query"] = @"public\s+int\s+Add\(",
                ["path"] = tempDir,
                ["is_regex"] = true,
            }, chatCtx).Result ?? "");

            Assert.That(resRegex, Does.Contain("Test1.cs:3:"));
            Assert.That(resRegex, Does.Contain("public int Add(int a, int b)"));
            Assert.That(resRegex, Does.Not.Contain("Test2.txt"));

            // Non-matching
            var resNone = (string)(ctx.Feature.Tools.GetTool("grep_search")!.Handler(new System.Text.Json.Nodes.JsonObject
            {
                ["query"] = "NonExistentSymbol_XYZ",
                ["path"] = tempDir,
            }, chatCtx).Result ?? "");

            Assert.That(resNone, Is.EqualTo("No matches found for 'NonExistentSymbol_XYZ'."));
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { /* best effort */ }
        }
    }
}
