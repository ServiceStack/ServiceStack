using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components.Tailwind;

public partial class HtmlFormat
{
    [Parameter, EditorRequired]
    public object? Value { get; set; }
}
