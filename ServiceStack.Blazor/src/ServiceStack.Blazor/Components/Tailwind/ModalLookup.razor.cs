﻿using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using ServiceStack.Text;

namespace ServiceStack.Blazor.Components.Tailwind;

/// <summary>
/// Dynamic Modal Lookup for selecting Referential Data
/// </summary>
/// <remarks>
/// ![](https://raw.githubusercontent.com/ServiceStack/docs/master/docs/images/blazor/components/DynamicModalLookup.png)
/// </remarks>
public partial class DynamicModalLookup : UiComponentBase
{
    [Parameter] public string Id { get; set; } = "DynamicModalLookup";
    [Parameter, EditorRequired] public AppMetadata? AppMetadata { get; set; }
    public RefInfo? RefInfo { get; set; }
    public bool Show { get; set; }
    public EventCallback<object> RowSelected { get; set; }
    public EventCallback Close { get; set; }
    public EventCallback<ModalLookup> Initialized { get; set; }
    public ModalLookup? ModalLookup { get; set; }

    void close()
    {
        Show = false;
        StateHasChanged();
    }

    public void CloseModal() => close();

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Close = EventCallback.Factory.Create(this, close);
        Initialized = EventCallback.Factory.Create<ModalLookup>(this, x => ModalLookup = x);
        RowSelected = EventCallback.Factory.Create<object>(this, async model => {
            close();
            if (Callback != null)
                await Callback(model);
        });
    }

    Func<object, Task>? Callback;

    public async Task OpenAsync(RefInfo? refInfo, Func<object, Task> callback)
    {
        //BlazorUtils.Log($"OpenAsync {refInfo.Dump()}, Initialized: {ModalLookup != null}");
        ModalLookup?.Reset();

        Show = true;
        RefInfo = refInfo;
        Callback = callback;

        if (ModalLookup != null)
        {
            await ModalLookup.UpdateAsync();
        }

        StateHasChanged();
    }

    string? CacheKey => RefInfo == null ? null : $"{RefInfo.Model}.{RefInfo.SelfId}.{RefInfo.RefId}.{RefInfo.RefLabel}.";

    class Context
    {
        public Type ModelType { get; }
        public Type RequestType { get; }
        public Type ComponentType { get; }
        public MetadataType MetadataType { get; }
        public Apis Apis { get; }
        public AttributeBuilder RowSelectedBuilder { get; }
        public Context(Type modelType, Type requestType, Type componentType, MetadataType metadataType, Apis apis, AttributeBuilder rowSelectedBuilder)
        {
            ModelType = modelType;
            RequestType = requestType;
            ComponentType = componentType;
            MetadataType = metadataType;
            Apis = apis;
            RowSelectedBuilder = rowSelectedBuilder;
        }
    }

    Dictionary<string, Context> cache = new();

    Context? Create()
    {
        var cacheKey = CacheKey;
        if (cacheKey == null)
            return null;

        if (cache.TryGetValue(cacheKey, out var context))
            return context;

        var metadataType = AppMetadata.GetType(RefInfo!.Model);
        if (metadataType == null)
        {
            BlazorUtils.LogError($"Could not find Type {RefInfo!.Model}");
            return null;
        }

        var requestType = RefInfo.QueryType ?? 
            (RefInfo.QueryApi != null
                ? Apis.Find(RefInfo.QueryApi)
                : null);
        if (requestType == null)
        {
            var queryOp = AppMetadata?.Api.FindAutoQueryReturning(RefInfo.Model);
            if (queryOp == null)
            {
                BlazorUtils.LogError($"Could not find Api Type for {RefInfo!.Model}");
                return null;
            }

            requestType = queryOp.Request.Type ??= Apis.Find(queryOp.Request.Name);
            if (requestType == null)
            {
                BlazorUtils.LogError($"Could not find Query Api Type for {queryOp.Request.Name}");
                return null;
            }
        }

        var genericDef = requestType.GetTypeWithGenericTypeDefinitionOfAny(typeof(QueryDb<>), typeof(QueryDb<,>));
        if (genericDef == null)
        {
            BlazorUtils.LogError($"Could not find generic QueryDb<> Type for {requestType?.Name} [{RefInfo.Model}]");
            return null;
        }

        var modelType = genericDef.FirstGenericArg();
        var componentType = typeof(ModalLookup<>).MakeGenericType(modelType);
        var apis = new Apis(new[] { requestType });

        var genericEventDef = typeof(EventCallbackBuilder<>).MakeGenericType(modelType);
        var rowSelectedBuilder = (AttributeBuilder)genericEventDef.GetConstructors().First().Invoke(new object[] { this.RowSelected });
        return cache[cacheKey] = new Context(modelType, requestType, componentType, metadataType, apis, rowSelectedBuilder);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var ctx = Create();
        if (ctx == null)
            return;

        builder.OpenComponent(1, ctx.ComponentType);
        builder.AddAttribute(2, nameof(Id), Id);
        builder.AddAttribute(3, nameof(Show), Show);
        builder.AddAttribute(4, nameof(ctx.Apis), ctx.Apis);
        builder.AddAttribute(5, nameof(Close), Close);
        builder.AddAttribute(6, nameof(Initialized), Initialized);

        ctx.RowSelectedBuilder.AddAttribute(builder, 7, nameof(RowSelected));

        builder.CloseComponent();
    }
}

public abstract class ModalLookup : AuthBlazorComponentBase
{
    abstract public void Reset();
    abstract public Task UpdateAsync();
}

/// <summary>
/// Modal Lookup for selecting Referential Data
/// </summary>
/// <remarks>
/// ![](https://raw.githubusercontent.com/ServiceStack/docs/master/docs/images/blazor/components/ModalLookup.png)
/// </remarks>
public partial class ModalLookup<Model> : ModalLookup
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
    [Parameter] public EventCallback Close { get; set; }
    [Parameter] public EventCallback<ModalLookup> Initialized { get; set; }

    string CacheKey => $"{Id}/{nameof(ApiPrefs)}/{typeof(Model).Name}";

    public ModalDialog? ModalDialog { get; set; }
    public DataGrid<Model>? DataGrid { get; set; } = default!;
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
    bool ShowQueryPrefs;
    bool apiLoading => Api == null;
    string? errorSummary => Api?.Error.SummaryMessage();
    int filtersCount => GetColumns().Select(x => x.Settings.Filters.Count).Sum();
    public List<AutoQueryConvention> FilterDefinitions { get; set; } = BlazorConfig.Instance.DefaultFilters;
    List<MetadataPropertyType> ViewModelColumns => Properties.Where(x => GetColumns().Any(c => c.Name == x.Name)).ToList();
    Column<Model>? Filter { get; set; }
    ApiPrefs ApiPrefs { get; set; } = new();
    
    async Task saveApiPrefs(ApiPrefs prefs)
    {
        ShowQueryPrefs = false;
        ApiPrefs = prefs;
        await LocalStorage!.SetItemAsync(CacheKey, ApiPrefs);
        await UpdateAsync();
    }

    bool hasPrefs => GetColumns().Any(c => c.Filters.Count > 0 || c.Settings.SortOrder != null)
        || ApiPrefs.SelectedColumns.Count > 0;
    async Task clearPrefs()
    {
        foreach (var c in GetColumns())
        {
            await c.RemoveSettingsAsync();
        }
        ApiPrefs.Clear();
        await LocalStorage!.RemoveAsync(CacheKey);
        await UpdateAsync();
    }


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

        await UpdateAsync();
    }

    async Task OnRowSelected(Model? item)
    {
        await RowSelected.InvokeAsync(item);
    }

    public override void Reset()
    {
        Api = null;
        open = null;
        Skip = 0;
        lastQuery = null;
        DataGrid?.SetSelectedItem(default);
    }

    string? lastQuery = null;
    public override async Task UpdateAsync()
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

        await Initialized.InvokeAsync(this);

        await UpdateAsync();
    }

    const int filterDialogWidth = 318;
    DOMRect FiltersTopLeftResolver(MouseEventArgs e)
    {
        return new DOMRect
        {
            X = Math.Max(Math.Floor(e.ClientX), filterDialogWidth),
            Y = 110,
        };
    }
}
