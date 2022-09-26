using Microsoft.AspNetCore.Components;
using ServiceStack.Text;

namespace ServiceStack.Blazor.Components.Tailwind;

public partial class LookupInput : TextInputBase, IHasJsonApiClient
{
    [Inject] public JsonApiClient? Client { get; set; }
    [Parameter, EditorRequired] public InputInfo Input { get; set; }
    [Parameter]
    public override string? Id
    {
        get => Input?.Id;
        set => Input!.Id = value;
    }

    [Parameter, EditorRequired] public MetadataType? MetadataType { get; set; }
    MetadataPropertyType? property;
    MetadataPropertyType Property => property ??= MetadataType!.Properties.First(x => x.Name == Input.Id);
    [Parameter, EditorRequired] public Dictionary<string, object> Model { get; set; }
    [Parameter] public string? LabelClass { get; set; }

    [CascadingParameter] public AppMetadata? AppMetadata { get; set; }
    ApiResult<AppMetadata> appMetadataApi = new();

    public string Value
    {
        get => Model.TryGetValue(Input!.Id, out var value) ? TextUtils.ToModelString(value) ?? "" : "";
        set => Model[Input!.Id] = value ?? "";
    }
    protected string UseId => Input!.Id!;
    protected override string UseType => Input!.Type ?? base.UseType;
    protected override string UsePlaceholder => Input!.Placeholder ?? base.UsePlaceholder;
    protected override string UseLabel => Input!.Label ?? base.UseLabel;
    protected override string UseHelp => Input!.Help ?? base.UseHelp;

    Type RefApiType => typeof(object);
    Type RefModelType => typeof(object);

    [CascadingParameter] public DynamicModalLookup? ModalLookup { get; set; }

    async Task lookup()
    {
        BlazorUtils.Log($"lookup: {ModalLookup != null}");

        if (ModalLookup == null)
            return;

        await ModalLookup.OpenAsync(Property.Ref, OnLookupSelected);
    }

    Task OnLookupSelected(object row)
    {
        if (row != null)
        {
            var refInfo = Property.Ref;
            //BlazorUtils.Log($"OnLookupSelected: {row.Dump()}, {refInfo.Dump()}; {MetadataType.Name}");
            var refModel = row.ToObjectDictionary();
            Model[Property.Name] = refModel.GetIgnoreCase(refInfo.RefId) ?? "";
            refInfoValue = LookupValues.SetValue(refInfo, refModel);
        }
        StateHasChanged();
        return Task.CompletedTask;
    }


    string? refInfoValue;
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        Model ??= new();
        if (!Model.ContainsKey(Property.Name))
        {
            Model[Property.Name] = "";
        }

        refInfoValue = null;
        var refInfo = Property.Ref;
        var refIdValue = refInfo.SelfId == null
            ? Model.GetIgnoreCase(Property.Name)?.ConvertTo<string>()
            : Model.GetIgnoreCase(refInfo.RefId)?.ConvertTo<string>();

        var isRefType = TextUtils.IsComplexType(refIdValue?.GetType());
        if (isRefType)
        {
            refIdValue = Model.GetIgnoreCase(refInfo.RefId).ConvertTo<string>();
        }
        if (refIdValue == null)
            return;

        var queryOp = AppMetadata?.Api.Operations.FirstOrDefault(x => x.DataModel?.Name == refInfo.Model);
        if (queryOp != null)
        {
            refInfoValue = Model.GetIgnoreCase(Property.Name).ConvertTo<string>();
            if (refInfo.RefLabel != null)
            {
                var colModel = MetadataType!.Properties.First(x => x.Type == refInfo.Model);
                var modelValue = colModel != null ? Model.GetIgnoreCase(colModel.Name).ToObjectDictionary() : null;
                if (modelValue != null)
                {
                    var label = modelValue.GetIgnoreCase(refInfo.RefLabel)?.ConvertTo<string>();
                    if (label != null)
                    {
                        refInfoValue = label;
                        LookupValues.SetValue(refInfo.Model, refIdValue, refInfo.RefLabel, label);
                    }
                    else
                    {
                        var isComputed = Property.Attributes?.Any(x => x.Name == "Computed") == true;
                        label = await LookupValues.GetOrFetchValueAsync(Client!, AppMetadata!, refInfo.Model, refIdValue, refInfo.RefLabel, isComputed);
                        refInfoValue = label != null ? label : $"{refInfo.Model}: {refInfoValue}";
                    }
                }
            }
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        if (AppMetadata == null)
        {
            appMetadataApi = await JsonApiClientUtils.ApiAppMetadataAsync(this);
            AppMetadata = appMetadataApi.Response;
        }
    }

    public ImageInfo Icon => AppMetadata!.Api.Types.FirstOrDefault(x => x.Name == Property.Ref.Model)?.Icon
        ?? BlazorConfig.Instance.DefaultTableIcon;
}

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
            ? (modelLookup.TryGetValue(id, out var idLookup)
                ? (idLookup.TryGetValue(label, out var value)
                    ? value
                    : null)
                : null)
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
                
                var dataModel = appMetadata.Api.Types.FirstOrDefault(x => x.Name == refModel);
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
