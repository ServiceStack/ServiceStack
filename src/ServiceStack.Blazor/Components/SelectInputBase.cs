using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components;

public class SelectInputBase<TValue> : TextInputBase<TValue>
{
    [Parameter]
    public IEnumerable<TValue>? Options { get; set; }

    protected List<KeyValuePair<string, string>> KvpValues() => TextUtils.ToKeyValuePairs(Options);
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
