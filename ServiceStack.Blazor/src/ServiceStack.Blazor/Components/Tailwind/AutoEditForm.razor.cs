using Microsoft.AspNetCore.Components;
using ServiceStack.Html;

namespace ServiceStack.Blazor.Components.Tailwind;

/// <summary>
/// Auto UI for generating a Edit Form from a Request DTO in a Slide Over component
/// </summary>
/// <remarks>
/// ![](https://raw.githubusercontent.com/ServiceStack/docs/master/docs/images/blazor/components/AutoEditForm.png)
/// </remarks>
/// <typeparam name="Model"></typeparam>
public partial class AutoEditForm<Model> : AutoFormBase<Model>
{
    [Parameter, EditorRequired] public Model Edit { get; set; }
    [Parameter] public Type? DeleteApiType { get; set; }

    protected override string Title => Heading ?? ApiType.GetDescription() ?? $"Edit {typeof(Model).Name} {MetadataType?.GetId(Edit)}";

    IHasErrorStatus? deleteApi;


    async Task OnDelete()
    {
        if (DeleteApiType == null)
            return;

        deleteApi = null;
        var request = CreateRequest(DeleteApiType);
        var model = request.ConvertTo<Model>();
        if (AutoSave)
        {
            api = await ApiAsync<Model>(request);
            if (api.Error != null)
            {
                await Error.InvokeAsync(api.Error);
                return;
            }
        }
        await Delete.InvokeAsync(model);
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        api = deleteApi = null;
        await TransitionAsync(show: true);

        ModelDictionary = Edit.ToModelDictionary();
        OriginalModelDictionary = new(ModelDictionary);
        FormLayout ??= MetadataType.CreateFormLayout(ApiType, AppMetadata);
    }
}
