using System.Text;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;

namespace ServiceStack.AI;

public partial class GeminiExtension
{
    static readonly (string Expr, string[] Expected, string Key, string Enables)[] CapabilityChecks =
    [
        ("status=\"published\"", ["alpha"], "equality", "baseline equality, lowercase key"),
        ("docType=\"guide\"", ["alpha"], "keyCamel", "camelCase keys"),
        ("doctype=\"guide\"", ["alpha"], "keyLower", "all-lowercase keys"),
        ("doc_type=\"guide\"", ["alpha"], "keySnake", "snake_case keys used by this extension"),
        ("versions:\"v8\"", ["alpha"], "listHas", "versions, tags and category subtree filters"),
        ("sortkey > 1700000000", ["alpha"], "numeric", "staleness filters"),
        ("sortKey > 1700000000", ["alpha"], "numericCamel", "numeric comparison on camelCase keys"),
        ("status=\"published\" AND versions:\"v8\"", ["alpha"], "and", "combining facets"),
        ("status=\"published\" OR status=\"deprecated\"", ["alpha", "beta"], "or", "multi-select facets"),
        ("NOT status=\"deprecated\"", ["alpha"], "not", "negative filters"),
    ];

    string CapabilitiesPath => Path.Combine(Ctx.GetUserPath("default"), "gemini", "capabilities.json");

    Task<object?> GetCapabilitiesAsync(ChatRequestContext req)
    {
        if (File.Exists(CapabilitiesPath))
        {
            try { return Task.FromResult<object?>(ChatJson.ParseObject(File.ReadAllText(CapabilitiesPath))); }
            catch (Exception e) { Log.LogWarning(e, "Could not read Gemini capability cache"); }
        }
        return Task.FromResult<object?>(new JsonObject
        {
            ["probed"] = false,
            ["note"] = "Not probed. Assuming full AIP-160 support; POST capabilities/probe to verify.",
            ["operators"] = new JsonObject(CapabilityChecks.Select(x =>
                KeyValuePair.Create<string, JsonNode?>(x.Key, true))),
            ["enables"] = new JsonObject(CapabilityChecks.Select(x =>
                KeyValuePair.Create<string, JsonNode?>(x.Key, x.Enables))),
        });
    }

    async Task<object?> ProbeCapabilitiesAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req).ConfigAwait();
        var result = await RunCapabilityProbeAsync().ConfigAwait();
        Directory.CreateDirectory(Path.GetDirectoryName(CapabilitiesPath)!);
        var temp = CapabilitiesPath + ".tmp";
        await File.WriteAllTextAsync(temp, result.ToJsonString(ChatJson.Indented) + "\n").ConfigAwait();
        File.Move(temp, CapabilitiesPath, true);
        return result;
    }

    async Task<JsonObject> RunCapabilityProbeAsync(CancellationToken token = default)
    {
        var store = await client.CreateFileSearchStoreAsync("llms-filter-probe", token).ConfigAwait();
        var storeName = store.GetString("name") ?? throw new Exception("Gemini did not create the probe store");
        var tempDir = Path.Combine(Path.GetTempPath(), "gemini-filter-probe-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(tempDir);
        var fixtures = new[]
        {
            (Key: "alpha", Text: "The Alpha widget costs exactly one hundred dollars and ships from Perth.",
                Type: "guide", Status: "published", Sort: 1755648000L, Versions: new[] { "v7", "v8" }),
            (Key: "beta", Text: "The Beta widget costs exactly two hundred dollars and ships from Sydney.",
                Type: "faq", Status: "deprecated", Sort: 1600000000L, Versions: new[] { "v6" }),
        };
        try
        {
            foreach (var fixture in fixtures)
            {
                var path = Path.Combine(tempDir, fixture.Key + ".txt");
                await File.WriteAllTextAsync(path, fixture.Text, token).ConfigAwait();
                JsonObject List(IEnumerable<string> values) => new()
                {
                    ["values"] = new JsonArray(values.Select(x => (JsonNode)x).ToArray()),
                };
                var metadata = new JsonArray(
                    new JsonObject { ["key"] = "docType", ["stringValue"] = fixture.Type },
                    new JsonObject { ["key"] = "doctype", ["stringValue"] = fixture.Type },
                    new JsonObject { ["key"] = "doc_type", ["stringValue"] = fixture.Type },
                    new JsonObject { ["key"] = "status", ["stringValue"] = fixture.Status },
                    new JsonObject { ["key"] = "sortKey", ["numericValue"] = fixture.Sort },
                    new JsonObject { ["key"] = "sortkey", ["numericValue"] = fixture.Sort },
                    new JsonObject { ["key"] = "versions", ["stringListValue"] = List(fixture.Versions) });
                var op = await client.UploadToFileSearchStoreAsync(storeName, path, new JsonObject
                {
                    ["displayName"] = fixture.Key + ".txt", ["customMetadata"] = metadata,
                }, MimeTypes.PlainText, token).ConfigAwait();
                op = await client.WaitForOperationAsync(op, token).ConfigAwait();
                if (op.GetObject("error") is { } error)
                    throw new Exception(error.GetString("message") ?? "Probe upload failed");
            }
            await Task.Delay(TimeSpan.FromSeconds(8), token).ConfigAwait();

            async Task<(HashSet<string> Found, string? Error)> CitedAsync(string? filter, int attempts)
            {
                var best = new HashSet<string>();
                for (var attempt = 0; attempt < attempts; attempt++)
                {
                    try
                    {
                        var search = new JsonObject
                        {
                            ["fileSearchStoreNames"] = new JsonArray(storeName), ["topK"] = 10,
                        };
                        if (filter != null) search["metadataFilter"] = filter;
                        var response = await client.GenerateContentAsync(
                            Ctx.Feature.ResolveVariable("$GEMINI_PROBE_MODEL") ?? "gemini-flash-latest",
                            new JsonObject
                            {
                                ["contents"] = new JsonArray(new JsonObject
                                {
                                    ["role"] = "user", ["parts"] = new JsonArray(new JsonObject
                                    { ["text"] = "According to the documents, list every widget and exactly what it costs." }),
                                }),
                                ["tools"] = new JsonArray(new JsonObject { ["fileSearch"] = search }),
                            }, token).ConfigAwait();
                        var found = new HashSet<string>();
                        foreach (var candidate in response.GetArray("candidates")?.OfType<JsonObject>() ?? [])
                        foreach (var chunk in candidate.GetObject("groundingMetadata")?.GetArray("groundingChunks")?.OfType<JsonObject>() ?? [])
                        {
                            var title = chunk.GetObject("retrievedContext")?.GetString("title")?.ToLowerInvariant() ?? "";
                            foreach (var fixture in fixtures) if (title.Contains(fixture.Key)) found.Add(fixture.Key);
                        }
                        if (found.Count > best.Count) best = found;
                        if (best.Count > 0) break;
                    }
                    catch (Exception e) { return (best, e.Message.SafeSubstring(0, 200)); }
                }
                return (best, null);
            }

            var baseline = await CitedAsync(null, 4).ConfigAwait();
            var enables = new JsonObject(CapabilityChecks.Select(x =>
                KeyValuePair.Create<string, JsonNode?>(x.Key, x.Enables)));
            if (baseline.Found.Count < fixtures.Length)
                return new JsonObject
                {
                    ["probed"] = false, ["probedAt"] = ChatDb.ToDateString(DateTime.UtcNow),
                    ["error"] = $"Baseline retrieval returned {string.Join(", ", baseline.Found)} instead of both fixtures."
                        + (baseline.Error != null ? " " + baseline.Error : ""),
                    ["operators"] = new JsonObject(CapabilityChecks.Select(x =>
                        KeyValuePair.Create<string, JsonNode?>(x.Key, true))), ["enables"] = enables,
                };

            var operators = new JsonObject(); var detail = new JsonObject();
            foreach (var check in CapabilityChecks)
            {
                var cited = await CitedAsync(check.Expr, 3).ConfigAwait();
                var matches = cited.Found.SetEquals(check.Expected);
                operators[check.Key] = matches;
                detail[check.Key] = new JsonObject
                {
                    ["expression"] = check.Expr,
                    ["expected"] = new JsonArray(check.Expected.Select(x => (JsonNode)x).ToArray()),
                    ["got"] = new JsonArray(cited.Found.OrderBy(x => x).Select(x => (JsonNode)x).ToArray()),
                    ["verdict"] = matches ? "ok" : cited.Error != null ? "error"
                        : cited.Found.Count == fixtures.Length ? "filter ignored" : "rejected or no match",
                    ["error"] = cited.Error,
                };
            }
            return new JsonObject
            {
                ["probed"] = true, ["probedAt"] = ChatDb.ToDateString(DateTime.UtcNow),
                ["model"] = Ctx.Feature.ResolveVariable("$GEMINI_PROBE_MODEL") ?? "gemini-flash-latest",
                ["operators"] = operators, ["enables"] = enables, ["detail"] = detail,
            };
        }
        finally
        {
            try { await client.DeleteFileSearchStoreAsync(storeName, token).ConfigAwait(); }
            catch (Exception e) { Log.LogWarning(e, "Could not delete Gemini capability probe store"); }
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }
}
