﻿using Microsoft.AspNetCore.Components;
using ServiceStack.Html;

namespace ServiceStack.Blazor.Components.Tailwind;

/// <summary>
/// Auto UI for generating a Create Form from a Request DTO in a Slide Over component
/// </summary>
/// <remarks>
/// ![](https://raw.githubusercontent.com/ServiceStack/docs/master/docs/images/blazor/components/AutoCreateForm.png)
/// </remarks>
/// <typeparam name="Model"></typeparam>
public partial class AutoCreateForm<Model> : AutoFormBase<Model>
{
    [Parameter] public Model? NewModel { get; set; }
    protected override string Title => Heading ?? ApiType.GetDescription() ?? $"New {typeof(Model).Name}";

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        api = null;
        await TransitionAsync(show: true);

        ModelDictionary = NewModel != null 
            ? ModelFilter(NewModel).ToModelDictionary() 
            : new();
        OriginalModelDictionary = new(ModelDictionary);
        FormLayout ??= MetadataType.CreateFormLayout(ApiType, AppMetadata);
        ConfigureFormLayout?.Invoke(FormLayout);
    }
}
