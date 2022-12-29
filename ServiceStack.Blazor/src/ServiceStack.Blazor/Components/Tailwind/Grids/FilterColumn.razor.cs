using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;

namespace ServiceStack.Blazor.Components.Tailwind;

public partial class FilterColumn<Model> : UiComponentBase
{
    [Parameter, EditorRequired] public Column<Model> Column { get; set; }
    [Parameter] public DOMRect? TopLeft { get; set; }
    [Parameter] public EventCallback Done { get; set; }
    [Parameter] public EventCallback<List<Filter>> Saved { get; set; }

    List<Filter> filters { get; set; } = new();

    FilterModel NewFilter = new();

    List<string> selectedEnums = new List<string>();

    string? newFilterValueType => Column.GetFilterRule(NewFilter.Value)?.ValueType;

    void selectedEnumsChanged(string enumValue, bool isChecked)
    {
        if (isChecked)
        {
            selectedEnums.Add(enumValue);
        }
        else
        {
            selectedEnums.RemoveAll(x => x == enumValue);
        }
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        filters = Column!.Settings!.Filters?.ToList() ?? new();
        if (Column.FieldType.IsEnum)
        {
            selectedEnums = Column!.Settings!.Filters?.FirstOrDefault()?.Values ?? new();
        }
    }

    void addFilter()
    {
        if (string.IsNullOrEmpty(NewFilter.Query))
            return;
        var name = Column.GetFilterRule(NewFilter.Query)?.Name;
        if (name == null)
            return;
        filters.Add(new Filter { Key = NewFilter.Query, Name = name, Value = NewFilter.Value });
        NewFilter.Query = NewFilter.Value = "";
        StateHasChanged();
    }

    void removeFilter(Filter filter)
    {
        filters.Remove(filter);
    }

    async Task onDone()
    {
        await Done.InvokeAsync();
    }

    async Task onSave()
    {
        if (Column.FieldType.IsEnum)
        {
            filters = selectedEnums.Count > 0
                ? new List<Filter> {
                    new Filter { Key = "%In", Name = "In", Values = selectedEnums },
                }
                : new List<Filter>();
        }
        else
        {
            if (!string.IsNullOrEmpty(NewFilter.Value))
            {
                addFilter();
            }
        }
        await Saved.InvokeAsync(filters);
        await onDone();
    }

    async Task sort(SortOrder sortOrder)
    {
        Column.Settings.SortOrder = Column.Settings.SortOrder == sortOrder ? null : sortOrder;
        await onSave();
    }

    void handleKeyUp(KeyboardEventArgs args)
    {
        if (args.Key == "Enter")
        {
            addFilter();
        }
    }
}
