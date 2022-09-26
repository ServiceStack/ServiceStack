using Microsoft.AspNetCore.Components;
using ServiceStack.Html;
using System.Security.AccessControl;

namespace ServiceStack.Blazor.Components.Tailwind;

/// <summary>
/// Auto UI for generating a chromelss Form from a Request DTO that can be embedded in custom Form UIs
/// ![](https://raw.githubusercontent.com/ServiceStack/docs/master/docs/images/blazor/components/AutoCreateForm.png)
/// </summary>
public partial class AutoFormFields<Model> : UiComponentBase
{
    string[] VisibleFields => FormLayout?.Where(x => x.Type != Input.Types.Hidden).Select(x => x.Id).ToArray() ?? Array.Empty<string>();
    [Parameter, EditorRequired] public IHasErrorStatus? Api { get; set; }
    [Parameter] public List<InputInfo>? FormLayout { get; set; }
    [Parameter, EditorRequired] public Dictionary<string, object> ModelDictionary { get; set; } = new();
    
    [CascadingParameter] public AppMetadata? AppMetadata { get; set; }
    protected MetadataType? metadataType;
    public MetadataType MetadataType => metadataType ??= AppMetadata?.Api.Types.FirstOrDefault(x => x.Name == typeof(Model).Name)
        ?? typeof(Model).ToMetadataType();

    protected override void OnInitialized()
    {
        base.OnInitialized();
        FormLayout ??= MetadataType.CreateFormLayout<Model>();
        Apis.Load(typeof(Model).Assembly);
    }
}
