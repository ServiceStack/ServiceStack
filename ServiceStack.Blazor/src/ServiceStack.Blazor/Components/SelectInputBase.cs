using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components;

public class SelectInputBase<TValue> : TextInputBase<TValue>
{
    [Parameter] public IEnumerable<TValue>? Options { get; set; }

    [Parameter] public InputInfo? Input { get; set; }

    [Parameter] public string[]? Values { get; set; }
    [Parameter] public KeyValuePair<string, string>[]? Entries { get; set; }

    protected List<KeyValuePair<string, string>> KvpValues()
    {
        if (Entries?.Length > 0)
            return Entries.ToList();
        
        if (Values?.Length > 0)
            return TextUtils.ToKeyValuePairs(Values);
        
        if (Input != null)
        {
            if (Input.AllowableEntries?.Length > 0)
                return Input.AllowableEntries.ToList();
        
            if (Input.AllowableValues?.Length > 0)
                return TextUtils.ToKeyValuePairs(Input.AllowableValues);
        }

        return Options != null
            ? TextUtils.ToKeyValuePairs(Options)
            : new();
    }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
