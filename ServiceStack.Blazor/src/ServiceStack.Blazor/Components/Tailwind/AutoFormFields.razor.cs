using Microsoft.AspNetCore.Components;
using ServiceStack.Html;

namespace ServiceStack.Blazor.Components.Tailwind;

/// <summary>
/// Auto UI for generating a chromelss Form from a Request DTO that can be embedded in custom Form UIs
/// <img src="https://raw.githubusercontent.com/ServiceStack/docs/master/docs/images/blazor/components/AutoCreateForm.png" />
/// </summary>
public partial class AutoFormFields : UiComponentBase
{
    string[] VisibleFields => FormLayout?.Where(x => x.Type != Input.Types.Hidden).Select(x => x.Id).ToArray() ?? Array.Empty<string>();
    [Parameter, EditorRequired] public IHasErrorStatus? Api { get; set; }
    [Parameter, EditorRequired] public List<InputInfo>? FormLayout { get; set; }
    [Parameter, EditorRequired] public Dictionary<string, object> ModelDictionary { get; set; } = new();
}
