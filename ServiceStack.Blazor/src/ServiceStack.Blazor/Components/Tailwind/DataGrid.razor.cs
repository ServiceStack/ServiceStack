using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace ServiceStack.Blazor.Components.Tailwind;

public partial class DataGrid<Model> : UiComponentBase
{
    public int InstanceId = BlazorUtils.NextId();
    [Inject] public LocalStorage LocalStorage { get; set; }
    [Parameter] public string Id { get; set; } = "DataGrid." + typeof(Model).Name;
    [Parameter] public RenderFragment<Column<Model>> Columns { get; set; }
    [Parameter] public List<AutoQueryConvention> FilterDefinitions { get; set; } = BlazorConfig.Instance.DefaultFilters;

    [Parameter]
    public ICollection<Model> Items { get; set; } = new List<Model>();

    [Parameter]
    public RenderFragment ChildContent { get; set; }

    [Parameter]
    public Func<Model, int, string> RowClass { get; set; }

    [Parameter] public bool AllowSelection { get; set; }
    [Parameter] public bool AllowFiltering { get; set; }

    [Parameter] public EventCallback<Column<Model>> HeaderSelected { get; set; }
    [Parameter] public EventCallback<Model?> RowSelected { get; set; }
    [Parameter] public string GridClass { get; set; } = CssDefaults.Grid.GridClass;
    [Parameter] public string HoverSelectionClass { get; set; } = CssDefaults.Grid.HoverSelectionClass;
    [Parameter] public string SelectedClass { get; set; } = CssDefaults.Grid.SelectedClass;
    [Parameter] public List<string>? SelectedColumns { get; set; }

    DOMRect? tableRect;
    [Inject] public IJSRuntime JS { get; set; }

    public List<Action> StateChangedHandlers { get; set; } = new();
    public async Task OnStateChanged() => await StateChanged.InvokeAsync();
    [Parameter] public EventCallback StateChanged { get; set; }

    protected Column<Model>? ShowFilters { get; set; }
    public DOMRect? ShowFiltersTopLeft { get; set; }
    protected ElementReference? refResults;

    protected Model? selectedItem;
    protected bool IsSelected(Model item) => selectedItem?.Equals(item) == true;
    protected string RowSelectionClass(Model item) => AllowSelection
        ? IsSelected(item) ? SelectedClass : HoverSelectionClass
        : "";

    const int filterDialogWidth = 318;


    [Parameter] public EventCallback<string> PropertyChanged { get; set; }
    [Parameter] public EventCallback<List<Filter>> FiltersChanged { get; set; }

    [CascadingParameter] public AppMetadata? AppMetadata { get; set; }
    protected MetadataType? metadataType;
    public MetadataType MetadataType => metadataType ??= AppMetadata?.Api.Types.FirstOrDefault(x => x.Name == typeof(Model).Name)
        ?? typeof(Model).ToMetadataType();

    internal async Task NotifyPropertyChanged(string propertyName)
    {
        await PropertyChanged.InvokeAsync(propertyName);
    }


    internal async Task OnHeaderSelected(MouseEventArgs e, Column<Model> column)
    {
        if (!AllowFiltering) return;
        ShowFilters = column;
        tableRect ??= await JS.InvokeAsync<DOMRect>("JS.invoke", new object[] { refResults!, "getBoundingClientRect" });
        ShowFiltersTopLeft = new DOMRect
        {
            X = Math.Floor(e.ClientX + filterDialogWidth / 2),
            Y = tableRect.Value.Y + 45,
        };

        StateHasChanged();
        await HeaderSelected.InvokeAsync(column);
    }

    protected async Task OnFilterDone()
    {
        ShowFilters = null;
        ShowFiltersTopLeft = null;
    }

    protected async Task OnFilterSave(List<Filter> filters)
    {
        ShowFilters!.Settings.Filters = filters;
        await ShowFilters.SaveSettingsAsync();
        await FiltersChanged.InvokeAsync();
    }

    public Model? SelectedItem => selectedItem;
    public async Task SetSelectedItem(Model model)
    {
        if (!AllowSelection) return;
        selectedItem = IsSelected(model) ? default : model;
        StateHasChanged();
        await RowSelected.InvokeAsync(selectedItem);
    }

    internal async Task OnRowSelected(MouseEventArgs e, Model model)
    {
        await SetSelectedItem(model);
    }


    protected readonly List<Column<Model>> columns = new();
    public IEnumerable<Column<Model>> VisibleColumns => SelectedColumns?.Count > 0
        ? columns.Where(c => SelectedColumns.Contains(c.Name))
        : columns;

    public List<Column<Model>> GetColumns() => columns;

    Dictionary<string, Column<Model>>? columnsMap;
    public Dictionary<string, Column<Model>> ColumnsMap => columnsMap ??= GetColumns().ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

    internal void AddColumn(Column<Model> column)
    {
        columns.Add(column);
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            if (columns.Count == 0)
            {
                foreach (var prop in MetadataType.Properties)
                {
                    columns.Add(new Column<Model>
                    {
                        DataGrid = this,
                        LocalStorage = LocalStorage,
                        PropertyAccessor = TypeProperties<Model>.GetAccessor(prop.Name),
                    });
                }
            }

            // Calling StateHasChanged() will re-render the component and populate the columns
            StateHasChanged();
        }
    }
}
