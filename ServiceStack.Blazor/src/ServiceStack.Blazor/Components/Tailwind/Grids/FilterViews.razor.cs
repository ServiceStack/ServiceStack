using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components.Tailwind;

public partial class FilterViews<Model> : UiComponentBase
{
    [Parameter, EditorRequired] public List<Column<Model>> Columns { get; set; } = new();

    [Parameter] public EventCallback Done { get; set; }
    [Parameter] public EventCallback FiltersChanged { get; set; }

    async Task removeFilters(Column<Model> column)
    {
        column.Filters.Clear();
        await column.SaveSettingsAsync();
        await FiltersChanged.InvokeAsync();
    }

    async Task removeFilter(Column<Model> column, Filter filter)
    {
        column.Settings.Filters.Remove(filter);
        await column.SaveSettingsAsync();
        await FiltersChanged.InvokeAsync();
    }

    async Task clearAll()
    {
        foreach (var c in Columns)
        {
            c.Settings.Filters.Clear();
            await c.SaveSettingsAsync();
        }
        await FiltersChanged.InvokeAsync();
        await Done.InvokeAsync();
    }
}
