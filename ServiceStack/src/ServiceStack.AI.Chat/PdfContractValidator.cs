using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace ServiceStack.AI;

public class PdfContractIssue
{
    public string Severity { get; set; } = "error";
    public string Code { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string? Path { get; set; }
    public string? Fixture { get; set; }
}

public class PdfContractValidation
{
    public bool IsValid => Issues.All(x => x.Severity != "error");
    public List<string> Fixtures { get; set; } = [];
    public List<PdfContractIssue> Issues { get; set; } = [];
}

/// <summary>Validates a PDF template's example/fixture JSON against its optional UI schema.</summary>
public partial class PdfContractValidator(PdfFeature feature)
{
    [GeneratedRegex(@"\bdata(?<path>(?:\.[A-Za-z_][A-Za-z0-9_-]*)+)")]
    private static partial Regex DataPathRegex();

    static readonly HashSet<string> SupportedKeywords = new(StringComparer.Ordinal)
    {
        "$schema", "$id", "$ref", "$defs", "definitions", "title", "description", "default", "examples",
        "type", "properties", "required", "additionalProperties", "items", "enum", "const", "allOf", "anyOf",
        "oneOf", "minimum", "maximum", "exclusiveMinimum", "exclusiveMaximum", "multipleOf", "minLength",
        "maxLength", "pattern", "minItems", "maxItems", "format", "x-titleKey",
    };

    public PdfContractValidation Validate(string root, string relPath, string publishedName)
    {
        var result = new PdfContractValidation();
        var typPath = PdfExtension.Resolve(root, relPath, mustExist: true);
        var dir = Path.GetDirectoryName(typPath)!;
        var stem = Path.GetFileNameWithoutExtension(typPath);
        var dataPath = Path.Combine(dir, stem + ".json");
        var schemaPath = Path.Combine(dir, stem + CoreToolsExtension.SchemaSuffix);

        var data = ReadJson(dataPath, "example", result);
        var schema = ReadJson(schemaPath, "schema", result);
        if (File.Exists(schemaPath) && !File.Exists(dataPath))
            Error(result, "MissingExample", "A .ui.json schema requires a sibling example .json file", "$");

        var fixturePrefix = stem + ".fixture.";
        foreach (var path in Directory.EnumerateFiles(dir, fixturePrefix + "*.json").OrderBy(x => x, StringComparer.Ordinal))
        {
            var fixture = Path.GetFileName(path)[fixturePrefix.Length..^".json".Length];
            if (fixture.Length == 0)
                continue;
            result.Fixtures.Add(fixture);
            var fixtureData = ReadJson(path, fixture, result);
            if (schema != null && fixtureData != null)
                ValidateNode(fixtureData, schema, schema, "$", fixture, result);
        }

        if (schema != null)
        {
            ReportUnsupportedKeywords(schema, "$", result);
            if (data != null)
                ValidateNode(data, schema, schema, "$", "example", result);
        }

        if (data != null)
            ValidateStaticDataPaths(File.ReadAllText(typPath), data, schema, result);

        if (data != null || schema != null)
        {
            try
            {
                new PdfCodeGen(feature).CreateFile(publishedName, feature.PdfCodeGen ?? new PdfCodeGenConfig(),
                    outputPath: "", data: data, schema: schema);
            }
            catch (Exception e)
            {
                Error(result, "CodeGeneration", $"The PDF data model cannot be generated: {e.Message}", "$");
            }
        }
        return result;
    }

    static JsonNode? ReadJson(string path, string fixture, PdfContractValidation result)
    {
        if (!File.Exists(path))
            return null;
        try
        {
            return ChatJson.Parse(File.ReadAllText(path));
        }
        catch (Exception e)
        {
            Error(result, "InvalidJson", $"{Path.GetFileName(path)} is not valid JSON: {e.Message}", "$", fixture);
            return null;
        }
    }

    static void ValidateNode(JsonNode? value, JsonNode schemaNode, JsonNode rootSchema, string path, string fixture,
        PdfContractValidation result)
    {
        if (schemaNode is not JsonObject schema)
            return;
        if (schema["$ref"]?.GetValue<string>() is { } reference)
        {
            if (ResolveRef(rootSchema, reference) is { } resolved)
                ValidateNode(value, resolved, rootSchema, path, fixture, result);
            else
                Error(result, "UnresolvedRef", $"Schema reference '{reference}' could not be resolved", path, fixture);
            return;
        }

        foreach (var child in schema["allOf"] as JsonArray ?? [])
            if (child != null) ValidateNode(value, child, rootSchema, path, fixture, result);
        ValidateChoice(value, schema["anyOf"] as JsonArray, rootSchema, path, fixture, "anyOf", result);
        ValidateChoice(value, schema["oneOf"] as JsonArray, rootSchema, path, fixture, "oneOf", result);

        var allowedTypes = ReadTypes(schema["type"]);
        if (allowedTypes.Count > 0 && !allowedTypes.Any(x => IsType(value, x)))
        {
            Error(result, "Type", $"Expected {string.Join(" or ", allowedTypes)}, found {TypeOf(value)}", path, fixture);
            return;
        }
        if (schema["const"] is { } constNode && !JsonNode.DeepEquals(value, constNode))
            Error(result, "Const", "Value does not match the schema constant", path, fixture);
        if (schema["enum"] is JsonArray values && !values.Any(x => JsonNode.DeepEquals(value, x)))
            Error(result, "Enum", "Value is not one of the allowed values", path, fixture);

        if (value is JsonObject obj)
            ValidateObject(obj, schema, rootSchema, path, fixture, result);
        else if (value is JsonArray array)
            ValidateArray(array, schema, rootSchema, path, fixture, result);
        else if (value is JsonValue scalar)
            ValidateScalar(scalar, schema, path, fixture, result);
    }

    static void ValidateObject(JsonObject obj, JsonObject schema, JsonNode rootSchema, string path, string fixture,
        PdfContractValidation result)
    {
        foreach (var required in schema["required"] as JsonArray ?? [])
        {
            var name = required?.GetValue<string>();
            if (name != null && !obj.ContainsKey(name))
                Error(result, "Required", $"Required property '{name}' is missing", Child(path, name), fixture);
        }
        var properties = schema["properties"] as JsonObject;
        if (properties != null)
        {
            foreach (var (name, childSchema) in properties)
                if (childSchema != null && obj.TryGetPropertyValue(name, out var child))
                    ValidateNode(child, childSchema, rootSchema, Child(path, name), fixture, result);
        }
        if (schema["additionalProperties"] is JsonValue additional && additional.TryGetValue<bool>(out var allowed) && !allowed)
        {
            foreach (var name in obj.Select(x => x.Key).Where(x => properties == null || !properties.ContainsKey(x)))
                Error(result, "AdditionalProperty", $"Property '{name}' is not declared by the schema", Child(path, name), fixture);
        }
    }

    static void ValidateArray(JsonArray array, JsonObject schema, JsonNode rootSchema, string path, string fixture,
        PdfContractValidation result)
    {
        CheckBound(schema, "minItems", array.Count, (actual, bound) => actual < bound, path, fixture, result);
        CheckBound(schema, "maxItems", array.Count, (actual, bound) => actual > bound, path, fixture, result);
        if (schema["items"] is { } itemSchema)
            for (var i = 0; i < array.Count; i++)
                ValidateNode(array[i], itemSchema, rootSchema, $"{path}[{i}]", fixture, result);
    }

    static void ValidateScalar(JsonValue value, JsonObject schema, string path, string fixture,
        PdfContractValidation result)
    {
        if (value.TryGetValue<string>(out var text))
        {
            CheckBound(schema, "minLength", text.Length, (actual, bound) => actual < bound, path, fixture, result);
            CheckBound(schema, "maxLength", text.Length, (actual, bound) => actual > bound, path, fixture, result);
            if (schema["pattern"]?.GetValue<string>() is { } pattern)
            {
                try { if (!Regex.IsMatch(text, pattern)) Error(result, "Pattern", $"Value does not match /{pattern}/", path, fixture); }
                catch (ArgumentException e) { Error(result, "InvalidPattern", e.Message, path, fixture); }
            }
            ValidateFormat(text, schema["format"]?.GetValue<string>(), path, fixture, result);
        }
        if (TryDecimal(value, out var number))
        {
            CompareNumber(schema, "minimum", number, (x, y) => x < y, path, fixture, result);
            CompareNumber(schema, "maximum", number, (x, y) => x > y, path, fixture, result);
            CompareNumber(schema, "exclusiveMinimum", number, (x, y) => x <= y, path, fixture, result);
            CompareNumber(schema, "exclusiveMaximum", number, (x, y) => x >= y, path, fixture, result);
            if (schema["multipleOf"] is JsonValue multipleNode && TryDecimal(multipleNode, out var multiple) &&
                multiple > 0 && number % multiple != 0)
                Error(result, "MultipleOf", $"Value must be a multiple of {multiple}", path, fixture);
        }
    }

    static void ValidateChoice(JsonNode? value, JsonArray? choices, JsonNode rootSchema, string path, string fixture,
        string keyword, PdfContractValidation result)
    {
        if (choices == null)
            return;
        var matches = 0;
        foreach (var choice in choices)
        {
            if (choice == null) continue;
            var probe = new PdfContractValidation();
            ValidateNode(value, choice, rootSchema, path, fixture, probe);
            if (probe.IsValid) matches++;
        }
        if ((keyword == "anyOf" && matches == 0) || (keyword == "oneOf" && matches != 1))
            Error(result, keyword, keyword == "anyOf" ? "Value does not match any allowed schema" :
                $"Value must match exactly one schema (matched {matches})", path, fixture);
    }

    static JsonNode? ResolveRef(JsonNode root, string reference)
    {
        if (!reference.StartsWith("#/", StringComparison.Ordinal))
            return null;
        JsonNode? node = root;
        foreach (var raw in reference[2..].Split('/'))
        {
            var part = raw.Replace("~1", "/").Replace("~0", "~");
            node = (node as JsonObject)?[part];
            if (node == null) return null;
        }
        return node;
    }

    static void ValidateStaticDataPaths(string source, JsonNode data, JsonNode? schema, PdfContractValidation result)
    {
        foreach (Match match in DataPathRegex().Matches(source))
        {
            var parts = match.Groups["path"].Value.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (PathExists(data, parts) || schema != null && SchemaPathExists(schema, parts))
                continue;
            Warning(result, "UnknownDataPath", $"Template reads '{match.Value}', which is absent from example data and schema",
                "$.'" + string.Join("'.'", parts) + "'");
        }
    }

    static bool PathExists(JsonNode? node, IEnumerable<string> parts)
    {
        foreach (var part in parts)
        {
            if (node is not JsonObject obj || !obj.TryGetPropertyValue(part, out node)) return false;
        }
        return true;
    }

    static bool SchemaPathExists(JsonNode? node, IEnumerable<string> parts)
    {
        foreach (var part in parts)
        {
            node = (node as JsonObject)?["properties"]?[part];
            if (node == null) return false;
        }
        return true;
    }

    static void ReportUnsupportedKeywords(JsonNode node, string path, PdfContractValidation result,
        bool memberNames = false)
    {
        if (node is JsonObject obj)
        {
            foreach (var (name, child) in obj)
            {
                if (!memberNames && !SupportedKeywords.Contains(name))
                    Warning(result, "UnsupportedKeyword", $"Schema keyword '{name}' is not enforced by publish validation", path);
                if (child != null) ReportUnsupportedKeywords(child, Child(path, name), result,
                    name is "properties" or "$defs" or "definitions");
            }
        }
        else if (node is JsonArray array)
            for (var i = 0; i < array.Count; i++) if (array[i] != null)
                ReportUnsupportedKeywords(array[i]!, $"{path}[{i}]", result);
    }

    static List<string> ReadTypes(JsonNode? node) => node switch
    {
        JsonValue value when value.TryGetValue<string>(out var type) => [type],
        JsonArray array => array.OfType<JsonValue>().Select(x => x.GetValue<string>()).ToList(),
        _ => [],
    };

    static bool IsType(JsonNode? node, string type) => type switch
    {
        "null" => node == null,
        "object" => node is JsonObject,
        "array" => node is JsonArray,
        "string" => node is JsonValue value && value.TryGetValue<string>(out _),
        "boolean" => node is JsonValue value && value.TryGetValue<bool>(out _),
        "integer" => node is JsonValue value && TryDecimal(value, out var number) && decimal.Truncate(number) == number,
        "number" => node is JsonValue value && TryDecimal(value, out _),
        _ => true,
    };

    static string TypeOf(JsonNode? node) => node switch
    {
        null => "null", JsonObject => "object", JsonArray => "array",
        JsonValue value when value.TryGetValue<string>(out _) => "string",
        JsonValue value when value.TryGetValue<bool>(out _) => "boolean",
        JsonValue value when TryDecimal(value, out var number) && decimal.Truncate(number) == number => "integer",
        JsonValue value when TryDecimal(value, out _) => "number", _ => "unknown",
    };

    static bool TryDecimal(JsonValue value, out decimal number)
    {
        if (value.TryGetValue<decimal>(out number)) return true;
        if (value.TryGetValue<double>(out var d) && decimal.TryParse(d.ToString("R", CultureInfo.InvariantCulture),
                NumberStyles.Float, CultureInfo.InvariantCulture, out number)) return true;
        number = 0; return false;
    }

    static void ValidateFormat(string value, string? format, string path, string fixture, PdfContractValidation result)
    {
        var valid = format switch
        {
            null or "" or "textarea" => true,
            "date" => DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
            "date-time" => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _),
            "uuid" => Guid.TryParse(value, out _),
            _ => true,
        };
        if (!valid) Error(result, "Format", $"Value is not a valid {format}", path, fixture);
    }

    static void CheckBound(JsonObject schema, string name, int actual, Func<int, int, bool> invalid, string path,
        string fixture, PdfContractValidation result)
    {
        if (schema[name] is JsonValue value && value.TryGetValue<int>(out var bound) && invalid(actual, bound))
            Error(result, name, $"Value violates {name} {bound}", path, fixture);
    }

    static void CompareNumber(JsonObject schema, string name, decimal actual, Func<decimal, decimal, bool> invalid,
        string path, string fixture, PdfContractValidation result)
    {
        if (schema[name] is JsonValue value && TryDecimal(value, out var bound) && invalid(actual, bound))
            Error(result, name, $"Value violates {name} {bound}", path, fixture);
    }

    static string Child(string path, string name) => path == "$" ? "$." + name : path + "." + name;
    static void Error(PdfContractValidation result, string code, string message, string? path = null, string? fixture = null) =>
        result.Issues.Add(new PdfContractIssue { Code = code, Message = message, Path = path, Fixture = fixture });
    static void Warning(PdfContractValidation result, string code, string message, string? path = null, string? fixture = null) =>
        result.Issues.Add(new PdfContractIssue { Severity = "warning", Code = code, Message = message, Path = path, Fixture = fixture });
}
