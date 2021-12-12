using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components.Tailwind;

public partial class SelectInput<TValue>
{
    [Parameter]
    public IEnumerable<TValue>? Options { get; set; }

    List<KeyValuePair<string, string>> kvpValues() => TextUtils.ToKeyValuePairs(Options);
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
