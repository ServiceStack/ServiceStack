using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components.Tailwind;

public partial class SettingsIcons<Model> : UiComponentBase
{
    [Parameter, EditorRequired] public Column<Model> Column { get; set; }
    [Parameter] public bool IsOpen { get; set; }
}
