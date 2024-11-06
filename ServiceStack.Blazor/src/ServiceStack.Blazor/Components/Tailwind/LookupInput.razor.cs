using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components.Tailwind;

/// <summary>
/// UX Friendly Input for selecting referential Data using Modal Lookup
/// </summary>
/// <remarks>
/// ![](https://raw.githubusercontent.com/ServiceStack/docs/master/docs/images/blazor/components/LookupInput.png)
/// </remarks>
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
    [Parameter, EditorRequired] public Dictionary<string, object?> Model { get; set; }
    [Parameter] public EventCallback Change { get; set; }

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
    public DynamicModalLookup? LocalModalLookup { get; set; }

    async Task lookup()
    {
        //BlazorUtils.LogDebug($"lookup: {ModalLookup != null}");

        var useModalLookup = ModalLookup ?? LocalModalLookup;
        if (useModalLookup == null)
            return;

        await useModalLookup.OpenAsync(Property.Ref, OnLookupSelected);
    }

    async Task OnLookupSelected(object row)
    {
        if (row != null)
        {
            var refInfo = Property.Ref;
            var refModel = row.ToObjectDictionary();
            Model[Input!.Id] = refModel.GetIgnoreCase(refInfo.RefId);
            refInfoValue = LookupValues.SetValue(refInfo, refModel);
            //BlazorUtils.LogDebug($"Model[{Input!.Id}] = {Model[Input!.Id]}, {refInfoValue} = ({refInfo.RefId}:{refInfo.RefLabel})");
            StateHasChanged();
            await Change.InvokeAsync();
        }
    }

    async Task Clear()
    {
        Model[Input!.Id] = null;
        refInfoValue = null;
        StateHasChanged();
        await Change.InvokeAsync();
    }

    string? refPropertyName;
    string? refInfoValue;
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        Model ??= new();
        if (!Model.ContainsKey(Input!.Id))
        {
            Model[Input!.Id] = null;
        }

        refInfoValue = null;
        //BlazorUtils.LogDebug("refInfoValue = null");
        var refInfo = Property.Ref;
        var refIdValue = refInfo.SelfId == null
            ? Model.GetIgnoreCase(Property.Name)?.ConvertTo<string>()
            : Model.GetIgnoreCase(refInfo.SelfId)?.ConvertTo<string>();

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
            var propValue = Model.GetIgnoreCase(Property.Name);
            if (TextUtils.IsComplexType(propValue?.GetType()))
                return;

            refInfoValue = propValue.ConvertTo<string>();
            //BlazorUtils.LogDebug("refInfoValue = {0}, RefLabel: {1} ", refInfoValue, refInfo.RefLabel);

            refPropertyName = Property.Name;
            if (refInfo.RefLabel != null)
            {
                var colModels = MetadataType!.Properties.Where(x => x.Type == refInfo.Model).ToList();
                if (colModels.Count == 0)
                {
                    BlazorUtils.LogError("Could not resolve {0} Property on {1}", refInfo.Model, MetadataType.Name);
                }

                var modelValues = colModels
                    .Select(x => Model.GetIgnoreCase(x.Name) as Dictionary<string,object>)
                    .Where(x => x != null)
                    .Select(x => x!)
                    .ToList();
                    
                var modelValue = colModels.Count == 0
                    ? null
                    : modelValues.Count == 1 
                        ? modelValues[0].ToObjectDictionary() 
                        : modelValues.FirstOrDefault(x => x.GetIgnoreCase(refInfo.RefId ?? "Id")?.ConvertTo<string>() == refIdValue)?.ToObjectDictionary();
                if (modelValue != null)
                {
                    var label = modelValue.GetIgnoreCase(refInfo.RefLabel)?.ConvertTo<string>();
                    if (label != null)
                    {
                        refInfoValue = label;
                        //BlazorUtils.LogDebug("{0} = LookupValues.SetValue({1},{2},{3})", label, refInfo.Model, refIdValue, refInfo.RefLabel);
                        LookupValues.SetValue(refInfo.Model, refIdValue, refInfo.RefLabel, label);
                    }
                }
                else
                {
                    var isComputed = Property.Attributes?.Any(x => x.Name == "Computed") == true;
                    var label = await LookupValues.GetOrFetchValueAsync(Client!, AppMetadata!, refInfo.Model, refInfo.RefId, refInfo.RefLabel, isComputed, refIdValue);
                    //BlazorUtils.LogDebug("{0} = LookupValues.GetOrFetchValueAsync({1},{2},{3},{4},{5})",
                    //    label ?? "null", refInfo.Model, refInfo.RefId, refInfo.RefLabel, isComputed, refIdValue);
                    refInfoValue = label != null ? label : $"{refInfo.Model}: {refInfoValue}";
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

    public ImageInfo Icon => AppMetadata!.GetType(Property.Ref.Model)?.Icon ?? BlazorConfig.Instance.DefaultTableIcon;
}
