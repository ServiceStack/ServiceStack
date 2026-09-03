using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace ServiceStack.AI;

public partial class GeminiDb
{
    public static readonly string[] FacetColumns =
        ["category", "docType", "status", "locale", "product", "versions", "tags", "categoryPath"];
    public static readonly string[] BulkColumns =
        ["category", "docType", "status", "locale", "product", "versions", "tags", "sourceUrl"];
    public static readonly string[] BulkListColumns = ["versions", "tags"];
    public static readonly string[] BulkOps = ["fill", "set", "clear", "add", "remove"];

    public List<ChatDocument> SelectDocuments(JsonObject selector, string? user, bool includeTombstoned = false)
    {
        var query = selector.GetObject("filter")?.Clone() ?? selector.Clone();
        if (selector.GetArray("ids") is { Count: > 0 } ids)
            query["ids_in"] = string.Join(',', ids.Select(x => x?.ToString()).Where(x => x != null));
        query.Remove("ids");
        query.Remove("filter");
        query["includeTombstoned"] = includeTombstoned;
        query["skip"] = 0;
        query["take"] = 1000;
        var ret = new List<ChatDocument>();
        while (true)
        {
            var page = QueryDocuments(query, user);
            ret.AddRange(page);
            if (page.Count < 1000) break;
            query["skip"] = query.GetInt("skip")!.Value + 1000;
        }
        return ret;
    }

    public JsonObject DocumentFacets(long filestoreId, IEnumerable<string>? requested, string? user)
    {
        var fields = (requested ?? FacetColumns).Where(FacetColumns.Contains).Distinct().ToList();
        var docs = SelectDocuments(new JsonObject { ["filestoreId"] = filestoreId }, user);
        var ret = new JsonObject();
        foreach (var field in fields)
        {
            var counts = new Dictionary<string, (int Count, long Size)>(StringComparer.Ordinal);
            var empty = 0;
            foreach (var doc in docs)
            {
                var values = FieldValues(doc, field);
                if (values.Count == 0) { empty++; continue; }
                foreach (var value in values.Distinct(StringComparer.Ordinal))
                {
                    var old = counts.GetValueOrDefault(value);
                    counts[value] = (old.Count + 1, old.Size + (doc.Size ?? 0));
                }
            }
            var facetValues = new JsonArray(counts.OrderByDescending(x => x.Value.Count).ThenBy(x => x.Key)
                .Select(x => (JsonNode)new JsonObject
                {
                    ["value"] = x.Key, ["count"] = x.Value.Count, ["size"] = x.Value.Size,
                }).ToArray());
            var facet = new JsonObject { ["values"] = facetValues, ["null"] = empty };
            if (field == "category")
                facet["tree"] = CategoryTree(facetValues, empty);
            ret[field] = facet;
        }
        return new JsonObject { ["total"] = docs.Count, ["facets"] = ret };
    }

    static JsonArray CategoryTree(JsonArray values, int rootCount)
    {
        var nodes = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
        JsonObject Node(string path)
        {
            if (!nodes.TryGetValue(path, out var node))
            {
                node = new JsonObject
                {
                    ["path"] = path, ["name"] = path.LastRightPart('/'),
                    ["own"] = 0, ["total"] = 0, ["children"] = new JsonArray(),
                };
                nodes[path] = node;
            }
            return node;
        }
        if (rootCount > 0)
        {
            var root = Node(""); root["name"] = "(root)"; root["own"] = rootCount; root["total"] = rootCount;
        }
        foreach (var row in values.OfType<JsonObject>())
        {
            var path = row.GetString("value");
            if (string.IsNullOrEmpty(path)) continue;
            var count = row.GetInt("count") ?? 0;
            Node(path)["own"] = Node(path).GetInt("own")!.Value + count;
            var parts = path.Split('/');
            for (var i = 1; i <= parts.Length; i++)
            {
                var parent = Node(string.Join('/', parts.Take(i)));
                parent["total"] = parent.GetInt("total")!.Value + count;
            }
        }
        var roots = new List<JsonObject>();
        foreach (var (path, node) in nodes.OrderBy(x => x.Key))
        {
            var pos = path.LastIndexOf('/');
            var parentPath = pos >= 0 ? path[..pos] : null;
            if (parentPath != null && nodes.TryGetValue(parentPath, out var parent))
                parent.GetArray("children")!.Add(node);
            else
                roots.Add(node);
        }
        return new JsonArray(roots.Select(x => (JsonNode)x).ToArray());
    }

    static List<string> FieldValues(ChatDocument doc, string field) => field switch
    {
        "category" => Scalar(doc.Category), "docType" => Scalar(doc.DocType),
        "status" => Scalar(doc.Status), "locale" => Scalar(doc.Locale), "product" => Scalar(doc.Product),
        "versions" => GeminiMetadata.AsList(doc.Versions), "tags" => GeminiMetadata.AsList(doc.Tags),
        "categoryPath" => GeminiMetadata.AsList(doc.CategoryPath), "sourceUrl" => Scalar(doc.SourceUrl),
        _ => [],
    };
    static List<string> Scalar(string? value) => string.IsNullOrEmpty(value) ? [] : [value];

    public static (object? Value, string Outcome) BulkApply(ChatDocument doc, string field, string op, JsonNode? value)
    {
        if (BulkListColumns.Contains(field))
        {
            var current = FieldValues(doc, field);
            var incoming = value is JsonArray array ? GeminiMetadata.AsList(array)
                : value is JsonValue v && v.TryGetValue<string>(out var str) ? GeminiMetadata.AsList(str) : [];
            List<string> next = op switch
            {
                "add" => current.Concat(incoming.Where(x => !current.Contains(x))).ToList(),
                "remove" => current.Where(x => !incoming.Contains(x)).ToList(),
                "clear" => [],
                _ => incoming,
            };
            return (next, current.SequenceEqual(next) ? "same" : "change");
        }
        var currentScalar = FieldValues(doc, field).FirstOrDefault();
        var incomingScalar = value?.GetValue<string>();
        if (op == "clear") return (null, currentScalar == null ? "same" : "change");
        if (op == "fill" && currentScalar != null) return (currentScalar, "skipped");
        return (incomingScalar, currentScalar == incomingScalar ? "same" : "change");
    }

    public JsonObject BulkPreview(List<ChatDocument> docs, JsonArray changes, bool apply = false)
    {
        var totals = new Dictionary<string, int> { ["selected"] = docs.Count, ["change"] = 0, ["same"] = 0, ["skipped"] = 0 };
        var fields = new JsonObject();
        var changedIds = new JsonArray();
        foreach (var change in changes.OfType<JsonObject>())
            fields[change.GetString("field")!] = new JsonObject { ["change"] = 0, ["same"] = 0, ["skipped"] = 0 };

        foreach (var doc in docs)
        {
            var anyChange = false; var anySkipped = false;
            foreach (var change in changes.OfType<JsonObject>())
            {
                var field = change.GetString("field")!;
                var (value, outcome) = BulkApply(doc, field, change.GetString("op") ?? "fill", change["value"]);
                var fieldStats = fields.GetObject(field)!;
                fieldStats[outcome] = fieldStats.GetInt(outcome)!.Value + 1;
                anyChange |= outcome == "change"; anySkipped |= outcome == "skipped";
                if (apply && outcome == "change") SetField(doc, field, value);
            }
            var overall = anyChange ? "change" : anySkipped ? "skipped" : "same";
            totals[overall]++;
            if (apply && anyChange)
            {
                SetSearchDesired(doc);
                UpdateDocument(doc);
                changedIds.Add(doc.Id);
            }
        }
        var ret = new JsonObject
        {
            ["selected"] = totals["selected"], ["change"] = totals["change"], ["same"] = totals["same"],
            ["skipped"] = totals["skipped"], ["fields"] = fields,
        };
        if (apply) { ret["changed"] = changedIds.Count; ret["ids"] = changedIds; }
        return ret;
    }

    static void SetField(ChatDocument doc, string field, object? value)
    {
        var scalar = value as string;
        var list = value is IEnumerable<string> items ? GeminiMetadata.JsonArrayOf(items) : null;
        switch (field)
        {
            case "category": doc.Category = scalar; break; case "docType": doc.DocType = scalar; break;
            case "status": doc.Status = scalar; break; case "locale": doc.Locale = scalar; break;
            case "product": doc.Product = scalar; break; case "sourceUrl": doc.SourceUrl = scalar; break;
            case "versions": doc.Versions = list; break; case "tags": doc.Tags = list; break;
        }
    }

    public JsonObject DocumentSummary(List<ChatDocument> docs, JsonArray? requestedFields)
    {
        var fieldNames = requestedFields == null ? BulkColumns
            : requestedFields.Select(x => x?.GetValue<string>()).Where(BulkColumns.Contains).Cast<string>().ToArray();
        var fields = new JsonObject();
        foreach (var field in fieldNames)
        {
            var counts = new Dictionary<string, int>(); var empty = 0;
            foreach (var doc in docs)
            {
                var values = FieldValues(doc, field);
                if (values.Count == 0) { empty++; continue; }
                foreach (var value in values) counts[value] = counts.GetValueOrDefault(value) + 1;
            }
            fields[field] = new JsonObject
            {
                ["values"] = new JsonArray(counts.OrderByDescending(x => x.Value).ThenBy(x => x.Key)
                    .Select(x => (JsonNode)new JsonObject { ["value"] = x.Key, ["count"] = x.Value }).ToArray()),
                ["empty"] = empty,
            };
        }
        return new JsonObject
        {
            ["count"] = docs.Count, ["fields"] = fields,
            ["sample"] = new JsonArray(docs.Take(8).Select(x => (JsonNode?)x.DisplayName).ToArray()),
        };
    }

    public List<(ChatDocument Doc, List<string> Fields)> PendingMetadata(long? filestoreId, string? user)
    {
        var selector = new JsonObject();
        if (filestoreId != null) selector["filestoreId"] = filestoreId.Value;
        var ret = new List<(ChatDocument, List<string>)>();
        foreach (var doc in SelectDocuments(selector, user))
        {
            // A queued upload has no remote metadata yet; it is upload progress, not a pending
            // metadata edit. Keeping the two queues separate is what makes the coverage count clear.
            if (doc.UploadedAt == null) continue;
            var remote = ChatDtos.ParseJson(doc.CustomMetadata) as JsonArray;
            var fields = GeminiMetadata.DiffFields(doc, remote);
            if (fields.Count > 0) ret.Add((doc, fields));
        }
        return ret;
    }
}
