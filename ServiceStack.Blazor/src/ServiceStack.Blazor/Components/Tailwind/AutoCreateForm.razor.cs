using Microsoft.AspNetCore.Components;
using ServiceStack.Html;

namespace ServiceStack.Blazor.Components.Tailwind;

public partial class AutoCreateForm<Model> : AutoFormBase<Model>
{
    [Parameter] public Model? NewModel { get; set; }
    protected override string Title => Heading ?? ApiType.GetDescription() ?? $"New {typeof(Model).Name}";

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        api = null;
        await TransitionAsync(show: true);

        ModelDictionary = NewModel.ToModelDictionary();
        FormLayout ??= MetadataType.CreateFormLayout();
    }
}
