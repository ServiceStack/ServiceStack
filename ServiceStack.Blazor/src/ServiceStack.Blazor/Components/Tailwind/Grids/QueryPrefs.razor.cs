using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components.Tailwind;

public partial class QueryPrefs : UiComponentBase
{
    int[] allTakes = new[] { 10, 25, 50, 100, 250, 500, 1000 };
    [Parameter] public string Id { get; set; } = "query-prefs";
    [Parameter, EditorRequired] public ApiPrefs Prefs { get; set; } = new();
    [Parameter, EditorRequired] public List<MetadataPropertyType> Columns { get; set; } = new();
    [Parameter, EditorRequired] public bool Show { get; set; }
    [Parameter] public EventCallback Done { get; set; }
    [Parameter] public EventCallback<ApiPrefs> Save { get; set; }
    ApiPrefs Model { get; set; } = new();

    async Task save()
    {
        await Save.InvokeAsync(Model);
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        Model = Prefs.Clone();
    }

    void columnSelected(MetadataPropertyType column, object? value)
    {
        if (value is bool b && b)
        {
            Model.SelectedColumns.Add(column.Name);
        }
        else
        {
            Model.SelectedColumns.Remove(column.Name);
        }
    }
}
