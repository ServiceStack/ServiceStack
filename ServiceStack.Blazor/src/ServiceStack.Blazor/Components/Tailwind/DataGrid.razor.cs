using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace ServiceStack.Blazor.Components.Tailwind;

/// <summary>
/// Display a typed .NET Collection
/// </summary>
/// <remarks>
/// ![](https://raw.githubusercontent.com/ServiceStack/docs/master/docs/images/blazor/components/DataGrid.png)
/// </remarks>
/// <typeparam name="Model"></typeparam>
public partial class DataGrid<Model> : UiComponentBase
{
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


    TableStyle tableStyle = CssDefaults.Grid.DefaultTableStyle;
    [Parameter] public TableStyle TableStyle 
    {
        get => tableStyle;
        set
        {
            tableStyle = value;
            GridClass = CssDefaults.Grid.GetGridClass(tableStyle);
            Grid2Class = CssDefaults.Grid.GetGrid2Class(tableStyle);
            Grid3Class = CssDefaults.Grid.GetGrid3Class(tableStyle);
            Grid4Class = CssDefaults.Grid.GetGrid4Class(tableStyle);
            TableHeadClass = CssDefaults.Grid.GetTableHeadClass(tableStyle);
            TableBodyClass = CssDefaults.Grid.GetTableBodyClass(tableStyle);
            TableHeaderRowClass = CssDefaults.Grid.GetTableHeaderRowClass(tableStyle);
            TableHeaderCellClass = CssDefaults.Grid.GetTableHeaderCellClass(tableStyle);
        }
    }

    [Parameter] public string GridClass { get; set; } = CssDefaults.Grid.GetGridClass();
    [Parameter] public string Grid2Class { get; set; } = CssDefaults.Grid.GetGrid2Class();
    [Parameter] public string Grid3Class { get; set; } = CssDefaults.Grid.GetGrid3Class();
    [Parameter] public string Grid4Class { get; set; } = CssDefaults.Grid.GetGrid4Class();
    [Parameter] public string TableClass { get; set; } = CssDefaults.Grid.GetTableClass();
    [Parameter] public string TableHeadClass { get; set; } = CssDefaults.Grid.GetTableHeadClass();
    [Parameter] public string TableHeaderRowClass { get; set; } = CssDefaults.Grid.GetTableHeaderRowClass();
    [Parameter] public string TableHeaderCellClass { get; set; } = CssDefaults.Grid.GetTableHeaderCellClass();
    [Parameter] public string TableBodyClass { get; set; } = CssDefaults.Grid.GetTableBodyClass();
    [Parameter] public List<string>? SelectedColumns { get; set; }
    [Parameter] public Func<MouseEventArgs, DOMRect>? FiltersTopLeftResolver { get; set; }
    [Parameter] public int MaxFieldLength { get; set; } = BlazorConfig.Instance.MaxFieldLength;

    DOMRect? tableRect;
    [Inject] public IJSRuntime JS { get; set; }

    public List<Action> StateChangedHandlers { get; set; } = new();
    async Task OnStateChanged() => await StateChanged.InvokeAsync();
    [Parameter] public EventCallback StateChanged { get; set; }

    Column<Model>? ShowFilters { get; set; }
    public DOMRect? ShowFiltersTopLeft { get; set; }
    ElementReference? refResults;

    Model? selectedItem;
    bool IsSelected(Model? item) => selectedItem?.Equals(item) == true;
    const int filterDialogWidth = 318;


    [Parameter] public EventCallback<string> PropertyChanged { get; set; }
    [Parameter] public EventCallback<List<Filter>> FiltersChanged { get; set; }

    [CascadingParameter] public AppMetadata? AppMetadata { get; set; }
    MetadataType? metadataType;
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
        tableRect = await JS.InvokeAsync<DOMRect>("JS.invoke", new object[] { refResults!, "getBoundingClientRect" });
        ShowFiltersTopLeft = FiltersTopLeftResolver?.Invoke(e) ?? new DOMRect
        {
            X = Math.Floor(e.ClientX + filterDialogWidth / 2),
            Y = tableRect.Value.Y + 45,
        };

        StateHasChanged();
        await HeaderSelected.InvokeAsync(column);
    }

    async Task OnFilterDone()
    {
        ShowFilters = null;
        ShowFiltersTopLeft = null;
    }

    async Task OnFilterSave(List<Filter> filters)
    {
        ShowFilters!.Settings.Filters = filters;
        await ShowFilters.SaveSettingsAsync();
        await FiltersChanged.InvokeAsync();
    }

    public Model? SelectedItem => selectedItem;
    public async Task SetSelectedItem(Model? model)
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


    readonly List<Column<Model>> columns = new();
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
                var props = AppMetadata?.GetAllProperties(typeof(Model).Name) ?? typeof(Model).GetAllMetadataProperties();
                foreach (var prop in props)
                {
                    var propAccessor = TypeProperties<Model>.GetAccessor(prop.Name);
                    columns.Add(new Column<Model>
                    {
                        DataGrid = this,
                        LocalStorage = LocalStorage,
                        PropertyAccessor = propAccessor,
                        MetadataProperty = propAccessor.PropertyInfo.ToMetadataPropertyType(),
                    });
                }
            }

            // Calling StateHasChanged() will re-render the component and populate the columns
            StateHasChanged();
        }
    }
}
