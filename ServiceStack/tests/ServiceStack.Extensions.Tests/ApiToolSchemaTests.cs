#nullable enable

using System.Text.Json.Nodes;
using NUnit.Framework;
using ServiceStack.Testing;
using ServiceStack.Text;

namespace ServiceStack.Extensions.Tests;

[NonParallelizable]
public class ApiDescribeSchemaTests
{
    class ToolResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    [Test]
    public void Api_describe_schema_is_the_Request_DTO_metadata_schema()
    {
        using var appHost = new BasicAppHost(typeof(MetadataSchemaService).Assembly).Init();

        var expected = MetadataSchemaGenerator.CreateSchema(typeof(MetadataSchema));
        var actual = JsonNode.Parse(ApiToolRegistry.CreateInputSchema(typeof(MetadataSchema)).ToJson());

        Assert.That(JsonNode.DeepEquals(actual, expected), Is.True);
        Assert.That(actual!["$id"]!.GetValue<string>(), Is.EqualTo("/api/MetadataSchema"));
        Assert.That(actual["request"]!.GetValue<string>(), Is.EqualTo(nameof(MetadataSchema)));
        Assert.That(actual["properties"]!["Name"]!["type"]!.GetValue<string>(), Is.EqualTo("string"));
    }

    [Test]
    public void Api_describe_can_include_Response_DTO_schema()
    {
        using var appHost = new BasicAppHost(typeof(MetadataSchemaService).Assembly).Init();

        var actual = JsonNode.Parse(ApiToolRegistry.CreateOutputSchema(typeof(ToolResponse)).ToJson());

        Assert.That(actual!["$id"]!.GetValue<string>(), Is.EqualTo("#ToolResponse"));
        Assert.That(actual["properties"]!["Id"]!["type"]!.GetValue<string>(), Is.EqualTo("integer"));
        Assert.That(actual["properties"]!["Name"]!["type"]!.GetValue<string>(), Is.EqualTo("string"));
    }

    class ModelWithDict
    {
        public int Id { get; set; }
        public System.Collections.Generic.Dictionary<string, string>? Meta { get; set; }
    }

    [Test]
    public void Metadata_schema_generator_creates_additionalProperties_for_dictionary()
    {
        var schema = MetadataSchemaGenerator.CreateModelSchema(typeof(ModelWithDict));
        var metaProp = schema["properties"]?["Meta"] as System.Text.Json.Nodes.JsonObject;
        Assert.That(metaProp, Is.Not.Null);
        Assert.That(metaProp!["type"]?.ToString(), Is.EqualTo("object"));
        var additionalProps = metaProp["additionalProperties"] as System.Text.Json.Nodes.JsonObject;
        Assert.That(additionalProps, Is.Not.Null);
        Assert.That(additionalProps!["type"]?.ToString(), Is.EqualTo("string"));
    }
}
