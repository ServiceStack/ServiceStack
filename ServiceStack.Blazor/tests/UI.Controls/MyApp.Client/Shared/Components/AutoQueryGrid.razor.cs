using Microsoft.AspNetCore.Components;
using ServiceStack;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;
using ServiceStack.Blazor;
using System.Runtime.Serialization;
using System.Xml.Linq;

namespace MyApp.Client.Components;

public class AutoQueryGridBase<Model> : AppAuthComponentBase
{
    public DataGridBase<Model>? DataGrid = default!;
    [Inject] public LocalStorage LocalStorage { get; set; }

    [Parameter] public string Id { get; set; } = "AutoQueryGrid";
    //[Parameter] public RenderFragment<Column<Model>> Columns { get; set; }
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
    public int? Total => Api?.Response?.Total;

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
    }

    protected Features? open { get; set; }

    public List<Column<Model>> GetColumns() => DataGrid?.GetColumns() ?? TypeConstants<Column<Model>>.EmptyList;

    protected int filtersCount => GetColumns().Select(x => x.Settings.Filters.Count).Sum();

    public List<MetadataPropertyType> ViewModelColumns => appMetadataApi.Response?.Api.Types
        .FirstOrDefault(x => x.Name == typeof(Model).Name)?.Properties ?? new();

    public Column<Model>? Filter { get; set; }
    public ApiPrefs ApiPrefs { get; set; } = new();
    protected async Task saveApiPrefs(ApiPrefs prefs)
    {
        ShowQueryPrefs = false;
        ApiPrefs = prefs;
        await LocalStorage.SetItemAsync(CacheKey, ApiPrefs);
    }

    ApiResult<AppMetadata> appMetadataApi = new();
    protected PluginInfo? Plugins => AppMetadata?.Plugins;

    protected AutoQueryInfo? Plugin => Plugins?.AutoQuery;

    protected bool ShowQueryPrefs;

    protected string? invalidAccess => null;
    protected string? invalidCreateAccess => null;
    protected string? invalidUpdateAccess => null;

    protected int Skip { get; set; } = 0;

    protected bool canFirst => true;
    protected bool canPrev => true;
    protected bool canNext => true;
    protected bool canLast => true;
    protected bool hasPrefs => GetColumns().Any(c => c.Filters.Count > 0 || c.Settings.SortOrder != null);

    //protected override async Task OnParametersSetAsync()
    //{
    //    await base.OnParametersSetAsync();
    //}

    protected async Task UpdateAsync()
    {
        var request = Apis!.QueryRequest<Model>();
        var requestWithReturn = (IReturn<QueryResponse<Model>>)request;
        Api = await ApiAsync(requestWithReturn);
        //Console.WriteLine("UpdateAsync: " + request.GetType().Name);
        //Console.WriteLine("Api.Succeeded: " + Api.Succeeded);
        //Console.WriteLine("Api: " + Api.ErrorSummary ?? Api.Error?.ErrorCode ?? $"{Api.Response?.Results?.Count ?? 0}");
        //if (Api.Succeeded)
        //{
        //    Console.WriteLine("Api.Body: " + Api.Response.Dump());
        //}
        StateHasChanged();
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

    public string CacheKey => $"{Id}/{nameof(ApiPrefs)}/{typeof(Model).Name}";

}
