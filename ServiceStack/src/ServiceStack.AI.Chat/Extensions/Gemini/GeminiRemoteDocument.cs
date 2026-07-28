using System.Globalization;

namespace ServiceStack.AI;

/// <summary>
/// A document as reported by Gemini, projected onto the fields the local row mirrors so uploads and
/// syncs share one mapping (port of the doc_to_dto/new_dto dicts in llms-py's gemini extension).
/// </summary>
public class GeminiRemoteDocument
{
    public string? Name { get; set; }
    public string? DisplayName { get; set; }
    public long? SizeBytes { get; set; }
    public string? MimeType { get; set; }
    public string? CreateTime { get; set; }
    public string? UpdateTime { get; set; }
    public string? State { get; set; }
    /// <summary>Python-shaped CustomMetadata[] JSON, e.g. [{"key":"id","numeric_value":1}]</summary>
    public string? CustomMetadata { get; set; }

    /// <summary>The document id recorded in Gemini's custom metadata (used to match local rows)</summary>
    public long? MetadataId { get; set; }
    public string? MetadataHash { get; set; }
    public string? MetadataCategory { get; set; }

    public static GeminiRemoteDocument From(JsonObject doc)
    {
        var customMetadata = doc.GetArray("customMetadata");
        var ret = new GeminiRemoteDocument
        {
            Name = doc.GetString("name"),
            DisplayName = doc.GetString("displayName"),
            SizeBytes = doc.GetLong("sizeBytes"),
            MimeType = doc.GetString("mimeType"),
            CreateTime = ToDbTime(doc.GetString("createTime")),
            UpdateTime = ToDbTime(doc.GetString("updateTime")),
            State = doc.GetString("state"),
            CustomMetadata = CustomMetadataDto(customMetadata)?.ToJsonString(ChatJson.Options),
        };

        foreach (var node in customMetadata ?? [])
        {
            if (node is not JsonObject meta)
                continue;
            switch (meta.GetString("key"))
            {
                case "id":
                    ret.MetadataId = meta.GetLong("numericValue");
                    break;
                case "hash":
                    ret.MetadataHash = meta.GetString("stringValue");
                    break;
                case "category":
                    ret.MetadataCategory = meta.GetString("stringValue");
                    break;
            }
        }
        return ret;
    }

    /// <summary>Fields whose local value differs from what Gemini reports</summary>
    public List<string> Diff(ChatDocument local)
    {
        var unmatched = new List<string>();
        if (local.Name != Name) unmatched.Add(nameof(Name));
        if (local.DisplayName != DisplayName) unmatched.Add(nameof(DisplayName));
        if (local.SizeBytes != SizeBytes) unmatched.Add(nameof(SizeBytes));
        if (local.MimeType != MimeType) unmatched.Add(nameof(MimeType));
        if (local.CreateTime != CreateTime) unmatched.Add(nameof(CreateTime));
        if (local.UpdateTime != UpdateTime) unmatched.Add(nameof(UpdateTime));
        if (local.State != State) unmatched.Add(nameof(State));
        if (local.CustomMetadata != CustomMetadata) unmatched.Add(nameof(CustomMetadata));
        return unmatched;
    }

    public void ApplyTo(ChatDocument local)
    {
        local.Name = Name;
        local.DisplayName = DisplayName;
        local.SizeBytes = SizeBytes;
        local.MimeType = MimeType;
        local.CreateTime = CreateTime;
        local.UpdateTime = UpdateTime;
        local.State = State;
        local.CustomMetadata = CustomMetadata;
    }

    /// <summary>"category/document.pdf" label used in the sync report</summary>
    public string FileName() => MetadataCategory != null
        ? $"{MetadataCategory}/{DisplayName}"
        : DisplayName ?? Name ?? "";

    /// <summary>Gemini CustomMetadata[] → the snake_case shape llms-py persists (and the UI receives)</summary>
    public static JsonArray? CustomMetadataDto(JsonArray? customMetadata)
    {
        if (customMetadata == null)
            return null;
        var ret = new JsonArray();
        foreach (var node in customMetadata)
        {
            if (node is not JsonObject meta)
                continue;
            var key = meta.GetString("key");
            if (meta.GetDouble("numericValue") is { } numericValue)
                ret.Add(new JsonObject { ["key"] = key, ["numeric_value"] = numericValue });
            else if (meta.GetObject("stringListValue")?.GetArray("values") is { } values)
                ret.Add(new JsonObject { ["key"] = key, ["string_list_value"] = values.Clone() });
            else if (meta.GetString("stringValue") is { } stringValue)
                ret.Add(new JsonObject { ["key"] = key, ["string_value"] = stringValue });
        }
        return ret;
    }

    /// <summary>Gemini's RFC3339 timestamps → the sortable string format the chat tables store</summary>
    public static string? ToDbTime(string? timestamp)
    {
        if (string.IsNullOrEmpty(timestamp))
            return null;
        return DateTimeOffset.TryParse(timestamp, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var date)
            ? ChatDb.ToDateString(date.UtcDateTime)
            : timestamp;
    }
}
