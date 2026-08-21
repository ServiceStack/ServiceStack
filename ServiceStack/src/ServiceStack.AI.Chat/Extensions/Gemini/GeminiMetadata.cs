using System.Globalization;
using System.Text.Json.Nodes;

namespace ServiceStack.AI;

/// <summary>Canonical Gemini custom metadata mapping and comparison.</summary>
public static class GeminiMetadata
{
    public record Field(string Column, string Key, string Kind);

    public static readonly Field[] Pushed =
    [
        new(nameof(ChatDocument.Id), "id", "numeric"),
        new(nameof(ChatDocument.Hash), "hash", "string"),
        new(nameof(ChatDocument.Category), "category", "string"),
        new(nameof(ChatDocument.CategoryPath), "category_path", "list"),
        new(nameof(ChatDocument.SourceUrl), "source_url", "string"),
        new(nameof(ChatDocument.DocType), "doc_type", "string"),
        new(nameof(ChatDocument.SourceUpdatedAt), "updated_at", "numeric"),
        new(nameof(ChatDocument.Status), "status", "string"),
        new(nameof(ChatDocument.Locale), "locale", "string"),
        new(nameof(ChatDocument.Product), "product", "string"),
        new(nameof(ChatDocument.Versions), "versions", "list"),
        new(nameof(ChatDocument.Tags), "tags", "list"),
    ];

    static GeminiMetadata()
    {
        var invalid = Pushed.Where(x => x.Key != x.Key.ToLowerInvariant()).Select(x => x.Key).ToList();
        if (invalid.Count > 0)
            throw new InvalidOperationException("Gemini custom metadata keys must be lowercase: " + string.Join(", ", invalid));
    }

    public static List<string> AsList(object? value)
    {
        if (value == null)
            return [];
        if (value is JsonArray array)
            return array.Select(x => x?.GetValue<string>()).Where(x => !string.IsNullOrEmpty(x)).Cast<string>().ToList();
        if (value is JsonObject obj)
            return AsList(obj.GetArray("values"));
        if (value is IEnumerable<string> strings)
            return strings.Where(x => !string.IsNullOrEmpty(x)).ToList();
        var text = Convert.ToString(value, CultureInfo.InvariantCulture);
        if (string.IsNullOrEmpty(text))
            return [];
        try
        {
            if (JsonNode.Parse(text) is JsonArray parsed)
                return AsList(parsed);
        }
        catch { /* a scalar string is a one-element list */ }
        return [text];
    }

    public static List<string> CategoryAncestors(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return [];
        var parts = category.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return Enumerable.Range(1, parts.Length).Select(i => string.Join('/', parts.Take(i))).ToList();
    }

    public static void NormalizeDocument(ChatDocument doc)
    {
        doc.SourceScopeId = doc.SourceId ?? 0;
        doc.CategoryPath = JsonArrayOf(CategoryAncestors(doc.Category));
        doc.Versions = JsonArrayOf(AsList(doc.Versions).SelectMany(SplitListInput));
        doc.Tags = JsonArrayOf(AsList(doc.Tags).SelectMany(SplitListInput));
    }

    static IEnumerable<string> SplitListInput(string value) => value.Contains(',')
        ? value.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0)
        : [value.Trim()];

    public static string JsonArrayOf(IEnumerable<string> values) =>
        new JsonArray(values.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase).Select(x => (JsonNode)x).ToArray())
        .ToJsonString(ChatJson.Options);

    static object? ValueOf(ChatDocument doc, string column) => column switch
    {
        nameof(ChatDocument.Id) => doc.Id,
        nameof(ChatDocument.Hash) => doc.Hash,
        nameof(ChatDocument.Category) => doc.Category,
        nameof(ChatDocument.CategoryPath) => doc.CategoryPath,
        nameof(ChatDocument.SourceUrl) => doc.SourceUrl,
        nameof(ChatDocument.DocType) => doc.DocType,
        nameof(ChatDocument.SourceUpdatedAt) => doc.SourceUpdatedAt,
        nameof(ChatDocument.Status) => doc.Status,
        nameof(ChatDocument.Locale) => doc.Locale,
        nameof(ChatDocument.Product) => doc.Product,
        nameof(ChatDocument.Versions) => doc.Versions,
        nameof(ChatDocument.Tags) => doc.Tags,
        _ => null,
    };

    public static JsonArray ToCustomMetadata(ChatDocument doc)
    {
        var ret = new JsonArray();
        foreach (var field in Pushed)
        {
            var value = ValueOf(doc, field.Column);
            if (field.Kind == "list")
            {
                var values = AsList(value);
                if (values.Count > 0)
                    ret.Add(new JsonObject
                    {
                        ["key"] = field.Key,
                        ["stringListValue"] = new JsonObject
                        {
                            ["values"] = new JsonArray(values.Select(x => (JsonNode)x).ToArray()),
                        },
                    });
            }
            else if (field.Kind == "numeric" && value != null)
            {
                ret.Add(new JsonObject
                {
                    ["key"] = field.Key,
                    ["numericValue"] = Convert.ToDouble(value, CultureInfo.InvariantCulture),
                });
            }
            else if (value is not null && Convert.ToString(value) is { Length: > 0 } text)
            {
                ret.Add(new JsonObject { ["key"] = field.Key, ["stringValue"] = text });
            }
        }
        return ret;
    }

    static Dictionary<string, string> Canonical(JsonArray? items)
    {
        var ret = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var item in items?.OfType<JsonObject>() ?? [])
        {
            var key = item.GetString("key");
            if (key == null) continue;
            var list = item.GetObject("stringListValue")?.GetArray("values")
                ?? item.GetObject("string_list_value")?.GetArray("values")
                ?? item.GetArray("string_list_value");
            if (list != null)
                ret[key] = "l:" + string.Join('\u001f', AsList(list).OrderBy(x => x, StringComparer.Ordinal));
            else if ((item.GetDouble("numericValue") ?? item.GetDouble("numeric_value")) is { } numeric)
                ret[key] = "n:" + GeminiNumeric(numeric).ToString("R", CultureInfo.InvariantCulture);
            else
                ret[key] = "s:" + (item.GetString("stringValue") ?? item.GetString("string_value") ?? "");
        }
        return ret;
    }

    public static double GeminiNumeric(double value)
    {
        var float32 = (float)value;
        return double.Parse(float32.ToString("G8", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
    }

    public static List<string> DiffFields(ChatDocument doc, JsonArray? remote)
    {
        var desired = Canonical(ToCustomMetadata(doc));
        var actual = Canonical(remote);
        return desired.Keys.Union(actual.Keys).Where(key =>
            !desired.TryGetValue(key, out var a) || !actual.TryGetValue(key, out var b) || a != b).ToList();
    }

    public static bool Differs(ChatDocument doc, JsonArray? remote) => DiffFields(doc, remote).Count > 0;
}
