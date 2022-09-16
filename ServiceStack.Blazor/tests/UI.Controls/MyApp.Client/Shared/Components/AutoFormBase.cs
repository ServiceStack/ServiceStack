using Microsoft.AspNetCore.Components;
using ServiceStack;
using ServiceStack.Html;
using ServiceStack.Blazor;

namespace MyApp.Client.Components;

public abstract class AutoFormBase<Model> : BlazorComponentBase
{
    [Parameter, EditorRequired] public Type ApiType { get; set; }
    [CascadingParameter] public AppMetadata? AppMetadata { get; set; }

    [Parameter] public string? Heading { get; set; }
    [Parameter] public string? SubHeading { get; set; }

    [Parameter] public string PanelClass { get; set; } = "pointer-events-auto w-screen xl:max-w-3xl md:max-w-xl max-w-lg";
    [Parameter] public string FormClass { get; set; } = "flex h-full flex-col divide-y divide-gray-200 bg-white shadow-xl";
    [Parameter] public string TitlebarClass { get; set; } = "bg-gray-50 px-4 py-6 sm:px-6";
    [Parameter] public string HeadingClass { get; set; } = "text-lg font-medium text-gray-900";

    [Parameter] public bool AutoSave { get; set; } = true;

    [Parameter] public EventCallback Done { get; set; }
    [Parameter] public EventCallback<Model> Save { get; set; }
    [Parameter] public EventCallback<Model> Delete { get; set; }
    [Parameter] public EventCallback<ResponseStatus> Error { get; set; }


    [Parameter] public List<InputInfo>? FormLayout { get; set; }

    protected string[] VisibleFields => FormLayout?.Where(x => x.Type != Input.Types.Hidden).Select(x => x.Id).ToArray() ?? Array.Empty<string>();
    protected Dictionary<string, object> ModelDictionary { get; set; } = new();

    protected DataTransition SlideOverTransition = new DataTransition(
        entering: new(@class: "transform transition ease-in-out duration-500 sm:duration-700", from: "translate-x-full", to: "translate-x-0"),
        leaving: new(@class: "transform transition ease-in-out duration-500 sm:duration-700", from: "translate-x-0", to: "translate-x-full"),
        visible: false);

    protected abstract string Title { get; }
    protected virtual string? Notes => ApiType.FirstAttribute<NotesAttribute>()?.Notes;

    protected MetadataType? metadataType;
    public MetadataType MetadataType => metadataType ??= AppMetadata?.Api.Types.FirstOrDefault(x => x.Name == ApiType.Name) 
        ?? ApiType.ToMetadataType();

    protected async Task OnDone()
    {
        await DataTransition.TransitionAllAsync(
            show: false,
            onChange: StateHasChanged,
            SlideOverTransition
        );
        await Task.Delay(500 - SlideOverTransition.DelayMs);
        await Done.InvokeAsync();
    }

    public object CreateRequest(Type type) => ModelDictionary.FromObjectDictionary(type);

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

    protected List<InputInfo> CreateFormLayout()
    {
        var formLayout = new List<InputInfo>();
        foreach (var prop in MetadataType.Properties)
        {
            if (prop.IsPrimaryKey == true)
                continue;

            if (prop.Input == null)
                prop.PopulateInput(Input.Create(prop.PropertyInfo));

            formLayout.Add(prop.Input!);
        }
        return formLayout;
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
