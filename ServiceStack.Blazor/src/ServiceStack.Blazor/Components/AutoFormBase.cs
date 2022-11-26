using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace ServiceStack.Blazor.Components;

public abstract class AutoFormBase<Model> : BlazorComponentBase
{
    [Parameter, EditorRequired] public Type ApiType { get; set; }

    [Parameter] public string? @class { get; set; }
    [Parameter] public string? Heading { get; set; }
    [Parameter] public string? SubHeading { get; set; }

    FormStyle formStyle = CssDefaults.Form.DefaultFormStyle;
    [Parameter]
    public FormStyle FormStyle
    {
        get => formStyle;
        set
        {
            formStyle = value;
            PanelClass = CssDefaults.Form.GetPanelClass(formStyle);
            FormClass = CssDefaults.Form.GetFormClass(formStyle);
            HeadingClass = CssDefaults.Form.GetHeadingClass(formStyle);
            SubHeadingClass = CssDefaults.Form.GetSubHeadingClass(formStyle);
        }
    }

    [Parameter] public string PanelClass { get; set; } = CssDefaults.Form.GetPanelClass();
    [Parameter] public string FormClass { get; set; } = CssDefaults.Form.GetFormClass();
    [Parameter] public string HeadingClass { get; set; } = CssDefaults.Form.GetHeadingClass();
    [Parameter] public string SubHeadingClass { get; set; } = CssDefaults.Form.GetSubHeadingClass();
    [Parameter] public string TitlebarClass { get; set; } = CssDefaults.Form.SlideOver.TitlebarClass;
    [Parameter] public string ButtonsClass { get; set; } = CssDefaults.Form.ButtonsClass;

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
    protected Dictionary<string, object> OriginalModelDictionary { get; set; } = new();

    protected DataTransition SlideOverTransition = CssDefaults.Form.SlideOver.SlideOverTransition;

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
            try
            {
                var pk = MetadataType.Properties.GetPrimaryKey();

                var formData = new MultipartFormDataContent();
                var reset = new List<string>();
                foreach (var entry in ModelDictionary)
                {
                    if (entry.Value is InputFileChangeEventArgs e)
                    {
                        var prop = MetadataType.Property(entry.Key);
                        var uploadInfo = prop?.UploadTo != null 
                            ? AppMetadata?.Plugins.FilesUpload.Locations.FirstOrDefault(x => x.Name == prop.UploadTo) 
                            : null;

                        var maxAllowedFiles = uploadInfo?.MaxFileCount ?? int.MaxValue;
                        var maxFileSize = uploadInfo?.MaxFileBytes ?? int.MaxValue;

                        var browserFiles = e.GetMultipleFiles(maxAllowedFiles);
                        foreach (var file in browserFiles)
                        {
                            formData.AddFile(entry.Key, file.Name, file.OpenReadStream(maxFileSize), mimeType: file.ContentType);
                        }
                    }
                    else
                    {
                        if (Crud.IsCrudPatch(ApiType))
                        {
                            var origValue = OriginalModelDictionary.GetIgnoreCase(entry.Key);
                            var isPk = pk?.Name != null && entry.Key.EqualsIgnoreCase(pk.Name);
                            var changed = origValue == null || entry.Value == null
                                ? origValue != entry.Value
                                : !origValue.Equals(entry.Value);

                            if (isPk || changed)
                            {
                                if (entry.Value != null)
                                {
                                    formData.AddParam(entry.Key, entry.Value);
                                }
                                else
                                {
                                    reset.Add(entry.Key);
                                }
                            }
                        }
                        else if (entry.Value != null)
                        {
                            formData.AddParam(entry.Key, entry.Value);
                        }
                    }
                }

                var url = request.GetType().ToApiUrl();
                if (reset.Count > 0 && request is IHasQueryParams queryParams)
                {
                    queryParams.AddQueryParam("reset", string.Join(',', reset));
                }

                api = await ApiFormAsync<Model>(request, formData);
                //api = await ApiAsync<Model>(request);
            }
            catch (Exception e)
            {
                api = ApiResult.CreateError<EmptyResponse>(e);
            }

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
