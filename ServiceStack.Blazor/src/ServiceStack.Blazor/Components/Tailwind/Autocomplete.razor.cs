using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using System.Linq.Expressions;
using Microsoft.JSInterop;

namespace ServiceStack.Blazor.Components.Tailwind;

public partial class Autocomplete<T> : TextInputBase
{
    [Inject] IJSRuntime JS { get; set; }
    [Parameter] public int ViewCount { get; set; } = 100;
    [Parameter] public int PageSize { get; set; } = 8;
    [Parameter] public bool Multiple { get; set; }

    [Parameter] public List<T> Options { get; set; } = new();
    [Parameter] public List<T> Values { get; set; } = new();
    [Parameter, EditorRequired] public Func<T, string, bool>? Match { get; set; }
    [Parameter, EditorRequired] public RenderFragment<T>? Item { get; set; }

    [Parameter] public T? Value { get; set; }
    [Parameter] public Expression<Func<T?>> ValueExpression { get; set; }
    [Parameter] public EventCallback<T?> ValueChanged { get; set; }

    [Parameter] public Expression<Func<List<T>>>? ValuesExpression { get; set; }
    [Parameter] public EventCallback<List<T>> ValuesChanged { get; set; }

    string? txtValue;
    bool showPopup = false;
    T? active;
    int take = int.MaxValue;

    List<T> FilteredValues = new();

    List<T> filterOptions() => Options == null
        ? new List<T>()
        : (string.IsNullOrEmpty(txtValue)
            ? Options
            : Options.Where(x => Match!(x, txtValue))).Take(take).ToList();

    string[] NavKeys = new[] { "Tab", "Escape", "ArrowDown", "ArrowUp", "Enter", "PageUp", "PageDown", "Home", "End" };

    void setActive(T item)
    {
        active = item;
        var currIndex = FilteredValues.IndexOf(active);
        if (currIndex > Math.Floor(take * .9))
        {
            take += ViewCount;
            refresh();
        }
    }

    char[] delims = new[] { ',', '\n', '\t' };

    async Task OnPaste(ClipboardEventArgs e)
    {
        // Need to wait for oninput to fire and update txtValue
        await Task.Delay(1);

        if (string.IsNullOrEmpty(txtValue))
            return;

        var multipleValues = txtValue.IndexOfAny(delims) >= 0;
        if (!Multiple || !multipleValues)
        {
            var matches = Options.Where(x => Match!(x, txtValue)).ToList();
            if (matches.Count == 1)
            {
                await select(matches[0]);
                showPopup = false;
                await JS.InvokeVoidAsync("JS.focusNextElement");
                StateHasChanged();
            }
        }
        else if (multipleValues)
        {
            var values = txtValue.Split(delims, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());
            var matches = Options.Where(x => values.Any(value => Match!(x, value))).ToList();
            if (matches.Count > 0)
            {
                foreach (var match in matches)
                {
                    await select(match);
                }
                showPopup = false;
                await JS.InvokeVoidAsync("JS.focusNextElement");
                StateHasChanged();
            }
        }
    }

    void OnKeyUp(KeyboardEventArgs e)
    {
        if (NavKeys.Contains(e.Code))
            return;

        update();
    }

    async Task OnKeyDown(KeyboardEventArgs e)
    {
        if (e.ShiftKey || e.CtrlKey || e.AltKey) return;

        if (!showPopup)
        {
            if (e.Code == "ArrowDown")
            {
                showPopup = true;
                active = FilteredValues.FirstOrDefault();
            }

            return;
        }

        if (e.Code == "Escape" || e.Code == "Tab")
        {
            showPopup = false;
        }
        else if (e.Code == "Home")
        {
            active = FilteredValues.FirstOrDefault();
            await scrollActiveIntoView();
        }
        else if (e.Code == "End")
        {
            active = FilteredValues.LastOrDefault();
            if (active != null)
                setActive(active);
            await scrollActiveIntoView();
        }
        else if (e.Code == "ArrowDown")
        {
            if (active == null)
            {
                active = FilteredValues.FirstOrDefault();
            }
            else
            {
                var currIndex = FilteredValues.IndexOf(active);
                active = currIndex + 1 < FilteredValues.Count
                    ? FilteredValues[currIndex + 1]
                    : FilteredValues.FirstOrDefault();
            }
            await onlyScrollActiveIntoViewIfNeeded();
        }
        else if (e.Code == "ArrowUp")
        {
            if (active == null)
            {
                active = FilteredValues.LastOrDefault();
            }
            else
            {
                var currIndex = FilteredValues.IndexOf(active);
                active = currIndex - 1 >= 0
                    ? FilteredValues[currIndex - 1]
                    : FilteredValues.LastOrDefault();
            }
            await onlyScrollActiveIntoViewIfNeeded();
        }
        else if (e.Code == "Enter")
        {
            if (active != null)
            {
                await select(active);
                if (!Multiple)
                {
                    await JS.InvokeVoidAsync("JS.focusNextElement");
                }
            }
            else
            {
                showPopup = false;
            }
        }
    }

    async Task scrollActiveIntoView(int delayMs = 10)
    {
        await JS.InvokeVoidAsync("JS.elInvokeDelay", $"#{Id}-autocomplete li.active", "scrollIntoView",
            new { behavior = "smooth", block = "nearest", inline = "nearest", scrollMode = "if-needed" }, delayMs);
    }

    async Task onlyScrollActiveIntoViewIfNeeded(int delayMs = 10)
    {
        await JS.InvokeVoidAsync("JS.elInvokeDelayIf", "scrollIntoViewIfNeeded", $"#{Id}-autocomplete li.active", "scrollIntoView",
            new { behavior = "smooth", block = "nearest", inline = "nearest", scrollMode = "if-needed" }, delayMs);
    }

    void FilterResults(KeyboardEventArgs e) => update();

    async Task toggle()
    {
        if (showPopup)
        {
            showPopup = false;
            return;
        }
        update();
        await JS.InvokeVoidAsync("JS.elInvoke", $"#{Id}", "focus");
    }

    void update()
    {
        showPopup = true;
        refresh();
        StateHasChanged();
    }

    async Task select(T option)
    {
        txtValue = null;
        showPopup = false;

        if (Multiple)
        {
            if (Values.Contains(option))
                Values.Remove(option);
            else
                Values.Add(option);

            active = default;
            await ValuesChanged.InvokeAsync(Values);
        }
        else
        {
            if (Values.Contains(option))
            {
                Value = default;
                Values.Remove(option);
            }
            else
            {
                Value = option;
                Values.Clear();
                Values.Add(Value);
            }
            await ValueChanged.InvokeAsync(Value);
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        take = ViewCount;
        await base.OnParametersSetAsync();
        refresh();
    }

    void refresh()
    {
        FilteredValues = filterOptions();
    }

    public override Task SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);

        if (!hasInitializedParameters)
        {
            // This is the first run
            // Could put this logic in OnInit, but its nice to avoid forcing people who override OnInit to call base.OnInit()
            if (ValueExpression != null)
            {
                FieldIdentifier = Microsoft.AspNetCore.Components.Forms.FieldIdentifier.Create(ValueExpression);
            }
            else if (ValuesExpression != null)
            {
                FieldIdentifier = Microsoft.AspNetCore.Components.Forms.FieldIdentifier.Create(ValuesExpression);
                Multiple = true;
            }
            else throw new InvalidOperationException($"{GetType()} requires a value for the 'ValueExpression' " +
                    $"parameter. Normally this is provided automatically when using 'bind-Value'.");

            Id ??= FieldIdentifier.FieldName;

            nullableUnderlyingType = Nullable.GetUnderlyingType(typeof(T));
            hasInitializedParameters = true;
        }

        // For derived components, retain the usual lifecycle with OnInit/OnParametersSet/etc.
        return base.SetParametersAsync(ParameterView.Empty);
    }
}
