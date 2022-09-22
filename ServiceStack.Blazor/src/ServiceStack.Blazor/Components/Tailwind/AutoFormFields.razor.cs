using Microsoft.AspNetCore.Components;
using ServiceStack.Html;

namespace ServiceStack.Blazor.Components.Tailwind;

public partial class AutoFormFields : UiComponentBase
{
    protected string[] VisibleFields => FormLayout?.Where(x => x.Type != Input.Types.Hidden).Select(x => x.Id).ToArray() ?? Array.Empty<string>();
    [Parameter, EditorRequired] public IHasErrorStatus? Api { get; set; }
    [Parameter, EditorRequired] public List<InputInfo>? FormLayout { get; set; }
    [Parameter, EditorRequired] public Dictionary<string, object> ModelDictionary { get; set; } = new();
}
