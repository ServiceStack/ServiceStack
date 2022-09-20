using Microsoft.AspNetCore.Components;
using ServiceStack;
using ServiceStack.Html;
using ServiceStack.Blazor;

namespace ServiceStack.Blazor.Components.Tailwind;

public abstract class AutoFormBase<Model> : BlazorComponentBase
{
    [Parameter, EditorRequired] public Type ApiType { get; set; }

    [Parameter] public string? Heading { get; set; }
    [Parameter] public string? SubHeading { get; set; }

    [Parameter] public string PanelClass { get; set; } = CssDefaults.Form.PanelClass;
    [Parameter] public string FormClass { get; set; } = CssDefaults.Form.FormClass;
    [Parameter] public string TitlebarClass { get; set; } = CssDefaults.Form.TitlebarClass;
    [Parameter] public string HeadingClass { get; set; } = CssDefaults.Form.HeadingClass;

    [Parameter] public bool AutoSave { get; set; } = true;

    [Parameter] public EventCallback Done { get; set; }
    [Parameter] public EventCallback<Model> Save { get; set; }
    [Parameter] public EventCallback<Model> Delete { get; set; }
    [Parameter] public EventCallback<ResponseStatus> Error { get; set; }

    [Parameter] public List<InputInfo>? FormLayout { get; set; }

    [CascadingParameter] public AppMetadata? AppMetadata { get; set; }
    protected MetadataType? metadataType;
    public MetadataType MetadataType => metadataType ??= AppMetadata?.Api.Types.FirstOrDefault(x => x.Name == ApiType.Name)
        ?? ApiType.ToMetadataType();

    protected Dictionary<string, object> ModelDictionary { get; set; } = new();

    protected DataTransition SlideOverTransition = CssDefaults.Form.SlideOverTransition;

    protected abstract string Title { get; }
    protected virtual string? Notes => ApiType.FirstAttribute<NotesAttribute>()?.Notes;

    protected async Task OnDone()
    {
        await CloseAsync();
        await Done.InvokeAsync();
    }

    public async Task CloseAsync()
    {
        await DataTransition.TransitionAllAsync(
            show: false,
            onChange: StateHasChanged,
            SlideOverTransition
        );
        await Task.Delay(500 - SlideOverTransition.DelayMs);
    }

    public object CreateRequest(Type type) => ModelDictionary.FromModelDictionary(type);

    protected IHasErrorStatus? api;
    protected virtual async Task OnSave()
    {
        api = null;
        var request = CreateRequest(ApiType);
        var model = request.ConvertTo<Model>();
        if (AutoSave)
        {
            api = await ApiAsync<Model>(request);
            if (api.Error != null)
            {
                await Error.InvokeAsync(api.Error);
                return;
            }
            var objApi = api.ToObjectDictionary();
            if (objApi.TryGetValue("Response", out var response))
            {
                var responseMap = response.ToObjectDictionary();
                // populates Id
                responseMap.PopulateInstance(responseMap);
                // populates Result
                if (responseMap.TryGetValue("Result", out var result))
                {
                    var resultMap = result.ToObjectDictionary();
                    resultMap.PopulateInstance(model);
                }
            }
        }
        await Save.InvokeAsync(model);
    }

    protected async Task TransitionAsync(bool show)
    {
        await DataTransition.TransitionAllAsync(
            show: show,
            onChange: StateHasChanged,
            SlideOverTransition
        );
    }
}
