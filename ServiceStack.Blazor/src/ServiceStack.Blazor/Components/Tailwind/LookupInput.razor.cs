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
            var refModel = row.ToObjectDictionary();
            Model[Property.Name] = refModel.GetIgnoreCase(refInfo.RefId);
            refInfoValue = LookupValues.SetValue(refInfo, refModel);
            StateHasChanged();
        }
        return Task.CompletedTask;
    }


    string? refPropertyName;
    string? refInfoValue;
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        Model ??= new();
        if (!Model.ContainsKey(Property.Name))
        {
            Model[Property.Name] = null;
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
            var propValue = Model.GetIgnoreCase(Property.Name);
            if (TextUtils.IsComplexType(propValue?.GetType()))
                return;

            refInfoValue = propValue.ConvertTo<string>();
            refPropertyName = Property.Name;
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
