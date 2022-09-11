using Microsoft.AspNetCore.Components;
using ServiceStack;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;
using ServiceStack.Blazor;
using System.Runtime.Serialization;
using System.Xml.Linq;
using System.Linq;

namespace MyApp.Client.Components;

public class AutoQueryGridBase<Model> : AppAuthComponentBase
{
    public DataGridBase<Model>? DataGrid = default!;
    public string CacheKey => $"{Id}/{nameof(ApiPrefs)}/{typeof(Model).Name}";
    [Inject] public LocalStorage LocalStorage { get; set; }
    [Inject] public NavigationManager NavigationManager { get; set; }

    [Parameter] public string Id { get; set; } = "AutoQueryGrid";
    [Parameter] public RenderFragment ChildContent { get; set; }
    [Parameter] public RenderFragment Columns { get; set; }
    [CascadingParameter] public AppMetadata? AppMetadata { get; set; }
    [Parameter] public bool AllowSelection { get; set; }
    [Parameter] public bool AllowFiltering { get; set; }
    public List<AutoQueryConvention> FilterDefinitions { get; set; } = ComponentConfig.DefaultFilters;
    [Parameter] public Apis? Apis { get; set; }

    [Parameter] public RenderFragment? Toolbar { get; set; }
    [Parameter] public bool ShowToolbar { get; set; } = true;
    [Parameter] public bool ShowPreferences { get; set; } = true;
    [Parameter] public bool ShowPagingNav { get; set; } = true;
    [Parameter] public bool ShowPagingInfo { get; set; } = true;
    [Parameter] public bool ShowDownloadCsv { get; set; } = true;
    [Parameter] public bool ShowCopyApiUrl { get; set; } = true;
    [Parameter] public bool ShowResetPreferences { get; set; } = true;
    [Parameter] public bool ShowFiltersView { get; set; } = true;
    [Parameter] public List<Model> Items { get; set; } = new();
    public List<Model> Results => Api?.Response?.Results ?? TypeConstants<Model>.EmptyList;
    public int Total => Api?.Response?.Total ?? Results.Count;

    public string ToolbarButtonClass { get; set; } = CssUtils.Tailwind.ToolbarButtonClass;

    protected ApiResult<QueryResponse<Model>>? Api { get; set; }

    protected bool apiLoading => Api?.IsLoading == true;
    protected string? errorSummary => Api?.Error.SummaryMessage();

    protected enum Features
    {
        Filters
    }

    protected async Task downloadCsv() { }
    protected async Task copyApiUrl() { }
    protected async Task clearPrefs() 
    {
        foreach (var c in GetColumns())
        {
            await c.RemoveSettingsAsync();
        }
        ApiPrefs.Clear();
        await LocalStorage.RemoveItemAsync(CacheKey);
        await UpdateAsync();
    }

    protected Features? open { get; set; }

    public List<Column<Model>> GetColumns() => DataGrid?.GetColumns() ?? TypeConstants<Column<Model>>.EmptyList;
    public Dictionary<string, Column<Model>> ColumnsMap => DataGrid?.ColumnsMap ?? new();

    protected int filtersCount => GetColumns().Select(x => x.Settings.Filters.Count).Sum();

    public List<MetadataPropertyType> Properties => appMetadataApi.Response?.Api.Types
        .FirstOrDefault(x => x.Name == typeof(Model).Name)?.Properties ?? new();
    public List<MetadataPropertyType> ViewModelColumns => Properties.Where(x => GetColumns().Any(c => c.Name == x.Name)).ToList();

    public Column<Model>? Filter { get; set; }
    public ApiPrefs ApiPrefs { get; set; } = new();
    protected async Task saveApiPrefs(ApiPrefs prefs)
    {
        ShowQueryPrefs = false;
        ApiPrefs = prefs;
        await LocalStorage.SetItemAsync(CacheKey, ApiPrefs);
        await UpdateAsync();
    }

    ApiResult<AppMetadata> appMetadataApi = new();
    protected PluginInfo? Plugins => AppMetadata?.Plugins;

    protected AutoQueryInfo? Plugin => Plugins?.AutoQuery;

    protected bool ShowQueryPrefs;

    protected string? invalidAccess => null;
    protected string? invalidCreateAccess => null;
    protected string? invalidUpdateAccess => null;

    [Parameter]
    [SupplyParameterFromQuery] public int Skip { get; set; } = 0;
    public int Take => ApiPrefs.Take;

    protected bool canFirst => Skip > 0;
    protected bool canPrev => Skip > 0;
    protected bool canNext => Results.Count >= Take;
    protected bool canLast => Results.Count >= Take;

    protected async Task skipTo(int value)
    {
        Skip += value;
        if (Skip < 0)
            Skip = 0;

        var lastPage = Math.Floor(Total / (double) Take) * Take;
        if (Skip > lastPage)
            Skip = (int)lastPage;

        string uri = NavigationManager.Uri.SetQueryParam("skip", Skip > 0 ? $"{Skip}" : null);
        log($"skipTo ({value}) {uri}");
        
        NavigationManager.NavigateTo(uri);
    }

    protected bool hasPrefs => GetColumns().Any(c => c.Filters.Count > 0 || c.Settings.SortOrder != null)
        || ApiPrefs.SelectedColumns.Count > 0;

    string? lastQuery = null;

    string? primaryKeyField;
    protected string PrimaryKeyField => primaryKeyField ??= Properties.First(c => c.IsPrimaryKey == true).Name;

    protected async Task UpdateAsync()
    {
        // PK always needed
        var selectFields = ApiPrefs.SelectedColumns.Count > 0 && !ApiPrefs.SelectedColumns.Contains(PrimaryKeyField)
            ? new List<string>(ApiPrefs.SelectedColumns) { PrimaryKeyField }
            : ApiPrefs.SelectedColumns;

        var selectedColumns = ApiPrefs.SelectedColumns.Count == 0
            ? GetColumns()
            : ApiPrefs.SelectedColumns.Select(x => ColumnsMap.TryGetValue(x, out var v) == true ? v : null)
                .Where(x => x != null).Select(x => x!).ToList();

        var orderBy = string.Join(",", selectedColumns.Where(x => x.Settings.SortOrder != null)
            .Select(x => x.Settings.SortOrder == SortOrder.Descending ? $"-{x.Name}" : x.Name));

        var fields = string.Join(',', selectFields);

        var filters = new Dictionary<string, string>();
        var sb = StringBuilderCache.Allocate();
        foreach (var column in GetColumns().Where(x => x.Settings.Filters.Count > 0))
        {
            foreach (var filter in column.Filters)
            {
                var key = filter.Key.Replace("%", column.Name);
                var value = filter.Value ?? (filter.Values != null ? string.Join(",", filter.Values) : "");
                filters[key] = value;
                sb.AppendQueryParam(key, value);
            }
        }
        var strFilters = StringBuilderCache.ReturnAndFree(sb);
        strFilters.TrimEnd('&');

        var newQuery = $"?skip={Skip}&take={Take}&fields={fields}&orderBy={orderBy}&{strFilters}";
        if (lastQuery == newQuery)
            return;
        lastQuery = newQuery;
        
        var request = Apis!.QueryRequest<Model>();
        request.Skip = Skip;
        request.Take = Take;
        request.Include = "Total";

        if (filters.Count > 0)
            request.QueryParams = filters;
        if (!string.IsNullOrEmpty(fields))
            request.Fields = fields;
        if (!string.IsNullOrEmpty(orderBy))
            request.OrderBy = orderBy;

        var requestWithReturn = (IReturn<QueryResponse<Model>>)request;
        Api = await ApiAsync(requestWithReturn);

        log($"UpdateAsync: {request.GetType().Name}{newQuery}, Succeeded: {Api.Succeeded}, Results: {Api.Response?.Results?.Count ?? 0}");
        if (!Api.Succeeded)
            log("Api: " + Api.ErrorSummary ?? Api.Error?.ErrorCode);

        StateHasChanged();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        await UpdateAsync();
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        if (AppMetadata == null)
        {
            appMetadataApi = await this.ApiAppMetadataAsync();
            AppMetadata = appMetadataApi.Response;
        }
        var autoQueryFilters = AppMetadata?.Plugins?.AutoQuery?.ViewerConventions;
        if (autoQueryFilters != null)
            FilterDefinitions = autoQueryFilters;

        ApiPrefs = await LocalStorage.GetItemAsync<ApiPrefs>(CacheKey) ?? new();

        await UpdateAsync();
    }
}
