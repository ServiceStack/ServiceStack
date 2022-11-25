using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components;

public class SelectInputBase<TValue> : TextInputBase<TValue>
{
    [Parameter] public IEnumerable<TValue>? Options { get; set; }

    [Parameter] public InputInfo? Input { get; set; }

    [Parameter] public string[]? Values { get; set; }
    [Parameter] public KeyValuePair<string, string>[]? Entries { get; set; }

    protected List<KeyValuePair<string, string>> KvpValues() => Entries?.Length > 0
        ? Entries.ToList()
        : Values?.Length > 0
            ? TextUtils.ToKeyValuePairs(Values)
            : Input?.AllowableEntries?.Length > 0
                ? Input!.AllowableEntries.ToList()
                : Input?.AllowableValues.Length > 0
                    ? TextUtils.ToKeyValuePairs(Input?.AllowableValues)
                    : TextUtils.ToKeyValuePairs(Options);

    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
