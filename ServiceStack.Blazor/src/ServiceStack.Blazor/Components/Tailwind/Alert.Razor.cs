using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components.Tailwind;

public partial class Alert : UiComponentBase
{
    [Parameter, EditorRequired] public string HtmlMessage { get; set; } = "";
}
