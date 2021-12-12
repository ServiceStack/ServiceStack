using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components.Bootstrap;

public partial class ErrorSummary
{
    [Parameter]
    public string[]? VisibleFields { get; set; }

    string[] UseVisibleFields => VisibleFields ?? Array.Empty<string>();
}
