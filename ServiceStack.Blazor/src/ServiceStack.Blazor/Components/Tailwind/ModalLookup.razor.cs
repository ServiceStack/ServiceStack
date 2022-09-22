using Microsoft.AspNetCore.Components;
using ServiceStack.Text;

namespace ServiceStack.Blazor.Components.Tailwind;

public partial class ModalLookup<Model> : AuthBlazorComponentBase
{
    [Inject] public LocalStorage? LocalStorage { get; set; }
    [Inject] public NavigationManager? NavigationManager { get; set; }
    [Inject] public Microsoft.JSInterop.IJSRuntime? JS { get; set; }

    [CascadingParameter] public AppMetadata? AppMetadata { get; set; }
    [Parameter] public bool Show { get; set; }
    [Parameter] public string Id { get; set; } = "ModalLookup";
    [Parameter] public EventCallback<Column<Model>> HeaderSelected { get; set; }
    [Parameter] public EventCallback<Model> RowSelected { get; set; }
    [Parameter] public Apis? Apis { get; set; }
    [Parameter] public RenderFragment ChildContent { get; set; }
    [Parameter] public RenderFragment Columns { get; set; }

    string CacheKey => $"{Id}/{nameof(ApiPrefs)}/{typeof(Model).Name}";

    DataGrid<Model>? DataGrid = default!;
    List<Column<Model>> GetColumns() => DataGrid?.GetColumns() ?? TypeConstants<Column<Model>>.EmptyList;
    Dictionary<string, Column<Model>> ColumnsMap => DataGrid?.ColumnsMap ?? new();
    [Parameter] public string ToolbarButtonClass { get; set; } = CssUtils.Tailwind.ToolbarButtonClass;

    int Skip { get; set; } = 0;
    int Take => ApiPrefs.Take;

    bool canFirst => Skip > 0;
    bool canPrev => Skip > 0;
    bool canNext => Results.Count >= Take;
    bool canLast => Results.Count >= Take;

    class QueryParams
    {
        public const string Skip = "skip";
        public const string Edit = "edit";
        public const string New = "new";
    }

    public List<Model> Results => Api?.Response?.Results ?? TypeConstants<Model>.EmptyList;
    public int Total => Api?.Response?.Total ?? Results.Count;
    ApiResult<QueryResponse<Model>>? Api { get; set; }
    public ApiPrefs ApiPrefs { get; set; } = new();
    bool ShowQueryPrefs;
    bool apiLoading => Api == null;
    string? errorSummary => Api?.Error.SummaryMessage();
    int filtersCount => GetColumns().Select(x => x.Settings.Filters.Count).Sum();
    public List<AutoQueryConvention> FilterDefinitions { get; set; } = BlazorConfig.Instance.DefaultFilters;

    Features? open { get; set; }
    enum Features
    {
        Filters
    }

    async Task skipTo(int value)
    {
        Skip += value;
        if (Skip < 0)
            Skip = 0;

        var lastPage = Math.Floor(Total / (double)Take) * Take;
        if (Skip > lastPage)
            Skip = (int)lastPage;

        string uri = NavigationManager!.Uri.SetQueryParam(QueryParams.Skip, Skip > 0 ? $"{Skip}" : null);
        log($"skipTo ({value}) {uri}");
        NavigationManager.NavigateTo(uri);
    }

    async Task OnRowSelected(Model? item)
    {
        await RowSelected.InvokeAsync(item);
    }

    string? lastQuery = null;
    async Task UpdateAsync()
    {
        var request = CreateRequestArgs(out var newQuery);
        if (lastQuery == newQuery)
            return;
        lastQuery = newQuery;

        var requestWithReturn = (IReturn<QueryResponse<Model>>)request;
        Api = await ApiAsync(requestWithReturn);

        log($"UpdateAsync: {request.GetType().Name}({newQuery}) Succeeded: {Api.Succeeded}, Results: {Api.Response?.Results?.Count ?? 0}");
        if (!Api.Succeeded)
            log("Api: " + Api.ErrorSummary ?? Api.Error?.ErrorCode);

        StateHasChanged();
    }

    ApiResult<AppMetadata> appMetadataApi = new();
    public List<MetadataPropertyType> Properties => appMetadataApi.Response?.Api.Types
        .FirstOrDefault(x => x.Name == typeof(Model).Name)?.Properties ?? new();
    MetadataPropertyType PrimaryKey => Properties.GetPrimaryKey()!;
    string? primaryKeyName;
    string PrimaryKeyName => primaryKeyName ??= PrimaryKey.Name;

    public QueryBase CreateRequestArgs() => CreateRequestArgs(out _);


    public QueryBase CreateRequestArgs(out string queryString)
    {
        // PK always needed
        var selectFields = ApiPrefs.SelectedColumns.Count > 0 && !ApiPrefs.SelectedColumns.Contains(PrimaryKeyName)
            ? new List<string>(ApiPrefs.SelectedColumns) { PrimaryKeyName }
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

        queryString = $"?skip={Skip}&take={Take}&fields={fields}&orderBy={orderBy}&{strFilters}";

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

        return request;
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        if (AppMetadata == null)
        {
            appMetadataApi = await ApiAppMetadataAsync();
            AppMetadata = appMetadataApi.Response;
        }
        var autoQueryFilters = AppMetadata?.Plugins?.AutoQuery?.ViewerConventions;
        if (autoQueryFilters != null)
            FilterDefinitions = autoQueryFilters;

        ApiPrefs = await LocalStorage!.GetItemAsync<ApiPrefs>(CacheKey) ?? new();

        await UpdateAsync();
    }

}
