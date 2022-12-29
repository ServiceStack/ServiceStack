using ServiceStack.Blazor.Components;
using ServiceStack.Text;

namespace ServiceStack.Blazor;

/// <summary>
/// Manage Lookup Data
/// </summary>
public static class LookupValues
{
    static Dictionary<string, Dictionary<string, Dictionary<string, string>>> Lookup = new();

    public static async Task<string?> GetOrFetchValueAsync(JsonApiClient client, AppMetadata appMetadata, string model, string id, string label, bool isComputed)
    {
        var value = GetValue(model, id, label);
        if (value != null)
            return value;

        await FetchAsync(client, appMetadata, model, id, label, isComputed, new List<string> { id });
        return GetValue(model, id, label);
    }

    public static string? GetValue(string model, string id, string label)
    {
        return Lookup.TryGetValue(model, out var modelLookup)
            ? modelLookup.TryGetValue(id, out var idLookup)
                ? idLookup.TryGetValue(label, out var value)
                    ? value
                    : null
                : null
            : null;
    }

    public static void SetValue(string model, string id, string label, string value)
    {
        var modelLookup = Lookup.TryGetValue(model, out var map)
            ? map
            : Lookup[model] = new();
        var idLookup = modelLookup.TryGetValue(id, out var idMap)
            ? idMap
            : modelLookup[id] = new();
        idLookup[label] = value;
        BlazorUtils.Log($"SetValue({model},{id},{label}) = {value}");
    }

    public static string? SetValue(RefInfo refInfo, Dictionary<string, object> refModel)
    {
        var id = refModel.GetIgnoreCase(refInfo?.RefId).ConvertTo<string>();
        if (id == null || refInfo?.RefLabel == null)
            return null;
        string value = refModel.GetIgnoreCase(refInfo.RefLabel).ConvertTo<string>();
        SetValue(refInfo.Model, id, refInfo.RefLabel, value);
        return value;
    }

    public static async Task FetchAsync(JsonApiClient client, AppMetadata appMetadata, List<Dictionary<string, object>> results, IEnumerable<MetadataPropertyType> props)
    {
        foreach (var prop in props)
        {
            var refId = prop.Ref?.RefId;
            var refLabel = prop.Ref?.RefLabel;
            var refModel = prop.Ref?.Model;
            if (refId != null && refLabel != null && refModel != null)
            {
                var lookupIds = results.Select(x => x.GetIgnoreCase(prop.Name).ConvertTo<string>()).Where(x => x != null).ToList();

                var dataModel = appMetadata.GetType(refModel);
                if (dataModel == null)
                {
                    BlazorUtils.Log($"Couldn't find AutoQuery Type for {refModel}");
                    continue;
                }
                var isComputed = prop.Attributes?.Any(x => x.Name == "Computed") == true
                    || dataModel.Properties?.FirstOrDefault(x => x.Name == refLabel)?.Attributes?.Any(x => x.Name == "Computed") == true;

                await FetchAsync(client, appMetadata, refModel, refId, refLabel, isComputed, lookupIds);
            }
        }
    }

    public static async Task FetchAsync(JsonApiClient client, AppMetadata appMetadata, string refModel, string refId, string refLabel, bool isComputed, List<string> lookupIds)
    {
        var lookupOp = appMetadata.Api.Operations.FirstOrDefault(op => op.Request.IsAutoQuery() && op.DataModel?.Name == refModel);
        if (lookupOp != null)
        {
            var modelLookup = Lookup.TryGetValue(refModel, out var map)
                ? map
                : Lookup[refModel] = new();

            var existingIds = new List<string>();
            foreach (var entry in modelLookup)
            {
                if (entry.Value.GetIgnoreCase(refLabel) != null)
                {
                    existingIds.Add(entry.Key);
                }
            }
            var newIds = lookupIds.Where(x => !existingIds.Contains(x)).ToList();
            if (newIds.Count == 0)
                return;

            var fields = !isComputed
                ? $"{refId},{refLabel}"
                : null;
            var queryArgs = new Dictionary<string, string>
            {
                [refId + "In"] = string.Join(',', newIds),
                ["jsconfig"] = "edv",
            };
            if (fields != null)
                queryArgs[nameof(fields)] = fields;

            var requestType = lookupOp.Request.Type ??= Apis.Find(lookupOp.Request.Name);
            if (requestType == null)
            {
                BlazorUtils.Log($"Couldn't find AutoQuery API Type for {lookupOp.Request.Name}");
                return;
            }

            var requestDto = (QueryBase)requestType.CreateInstance();

            var responseType = requestType.GetResponseType();
            requestDto.QueryParams = queryArgs;
            try
            {
                var response = await client.SendAsync(responseType, requestDto);
                var lookupResults = response.ToObjectDictionary()["Results"] as System.Collections.IEnumerable;

                BlazorUtils.Log($"Querying {requestType.Name} {queryArgs.Dump()} -> {EnumerableUtils.Count(lookupResults)}");

                foreach (var obj in lookupResults.OrEmpty())
                {
                    var result = obj.ToObjectDictionary();
                    var id = result.GetIgnoreCase(refId).ConvertTo<string>();
                    var val = result.GetIgnoreCase(refLabel);

                    var modelLookupLabels = modelLookup.TryGetValue(id, out var idMap)
                        ? idMap
                        : modelLookup[id] = new();
                    modelLookupLabels[refLabel] = val.ConvertTo<string>();
                    BlazorUtils.Log($"SetFetch({refModel},{id},{refLabel}) = {modelLookupLabels[refLabel]}");
                }
            }
            catch (Exception)
            {
                BlazorUtils.Log($"Failed to call {requestDto.GetType().Name} -> {responseType?.Name}");
                return;
            }
        }
        else
        {
            BlazorUtils.Log($"Couldn't find AutoQuery API for {refModel}");
            return;
        }
    }
}
