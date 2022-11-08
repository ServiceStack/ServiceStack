using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components;

public class SelectInputBase<TValue> : TextInputBase<TValue>
{
    [Parameter] public IEnumerable<TValue>? Options { get; set; }

    [Parameter] public InputInfo? Input { get; set; }

    protected List<KeyValuePair<string, string>> KvpValues() => Input?.AllowableEntries?.Length > 0 
        ? Input!.AllowableEntries.ToList()
        : Input?.AllowableValues.Length > 0 
            ? TextUtils.ToKeyValuePairs(Input?.AllowableValues)
            : TextUtils.ToKeyValuePairs(Options);

    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
