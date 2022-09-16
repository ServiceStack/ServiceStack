using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using System.Collections;
using ServiceStack.Blazor;

namespace ServiceStack.Blazor.Components.Tailwind;

public class Column<Model> : UiComponentBase
{
    [Inject] public LocalStorage LocalStorage { get; set; }
    public int InstanceId = BlazorUtils.NextId();

    [CascadingParameter] public DataGridBase<Model>? DataGrid { get; set; }
    //[CascadingParameter] public AutoQueryGridBase<Model>? AutoQueryGrid { get; set; }
    [Parameter] public Expression<Func<Model, object>>? Field { get; set; }
    [Parameter] public string? FieldName { get; set; }
    [Parameter] public string? HeaderClass { get; set; }
    [Parameter] public Breakpoint? HeaderBreakpoint { get; set; }
    [Parameter] public string? CellClass { get; set; }
    [Parameter] public string? Title { get; set; }
    [Parameter] public string? Format { get; set; }
    [Parameter] public Func<object, string>? Formatter { get; set; }
    [Parameter] public Breakpoint? VisibleFrom { get; set; }
    [Parameter] public ColumnSettings Settings { get; set; } = new();

    // Use the provided title or infer it from the expression
    public string? Label => Title ?? (Field != null ? GetMemberName(Field).SplitCamelCase().ToTitleCase() : null);

    protected override void OnInitialized()
    {
        base.OnInitialized();
        //Console.WriteLine($"DataGrid: {DataGrid != null}, AutoQueryGrid: {AutoQueryGrid != null}");
        //DataGrid ??= AutoQueryGrid!.DataGrid;
        //if (DataGrid == null)
        //    throw new ArgumentNullException(nameof(DataGrid));
        DataGrid!.AddColumn(this);
    }

    [Parameter] public RenderFragment? Header { get; set; }
    [Parameter] public RenderFragment<Model>? Template { get; set; }
    private RenderFragment headerTemplate;
    private RenderFragment<Model>? cellTemplate;
    private Expression? lastCompiledExpression;
    private Func<Model, object>? compiledExpression;
    public List<Filter> Filters => Settings?.Filters ?? TypeConstants<Filter>.EmptyList;

    protected override void OnParametersSet()
    {
        if (lastCompiledExpression != Field)
        {
            compiledExpression = Field?.Compile();
            lastCompiledExpression = Field;
        }
    }

    public string CacheKey => $"Column/{DataGrid?.Id ?? "DataGrid"}:{typeof(Model).Name}.{Name}";

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        Settings = await LocalStorage.GetItemAsync<ColumnSettings>(CacheKey) ?? new();
        await DataGrid!.NotifyPropertyChanged(nameof(Settings));
        await DataGrid!.FiltersChanged.InvokeAsync();
        //Console.WriteLine($"LOAD {CacheKey}: {Settings.Filters.Count}");
    }

    public async Task SaveSettingsAsync()
    {
        await LocalStorage.SetItemAsync(CacheKey, Settings);
        await DataGrid!.NotifyPropertyChanged(nameof(Settings));
        //Console.WriteLine($"SAVE {CacheKey}: {Settings.Filters.Count}");
    }

    public async Task RemoveSettingsAsync()
    {
        Settings.Clear();
        await LocalStorage.RemoveItemAsync(CacheKey);
        await DataGrid!.NotifyPropertyChanged(nameof(Settings));
    }

    public string Name => Field != null ? GetMemberName(Field) : FieldName ?? throw new Exception("Field or FieldName needs to be set");
    public Type FieldType => (Field != null ? GetMemberType(Field) : null) ?? typeof(object);
    public KeyValuePair<string, string>[] EnumEntries => FieldType.IsEnum
        ? Html.Input.GetEnumEntries(FieldType)
        : Array.Empty<KeyValuePair<string, string>>();

    public List<AutoQueryConvention> Definitions => DataGrid?.FilterDefinitions ?? TypeConstants<AutoQueryConvention>.EmptyList;

    public string FormatValue(Type type, string value)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsValueType
            ? value.ToString()
            : type != typeof(string) && value is IEnumerable
                ? $"[{value}]"
                : $"'{value}'";
    }

    public List<AutoQueryConvention> FilterRules => FieldType != typeof(string)
        ? Definitions.Where(x => x.Types != "string").ToList()
        : Definitions;

    public AutoQueryConvention? GetFilterRule(string value) =>
        FilterRules.FirstOrDefault(x => x.Value == value);

    public string GetFilterValue(Filter filter) => X.Map(GetFilterRule(filter.Key), rule => rule.ValueType == "None"
        ? ""
        : filter.Values != null
            ? $"({string.Join(",", filter.Values)})"
            : FormatValue(FieldType, filter.Value!)) ?? "";

    internal RenderFragment HeaderTemplate
    {
        get
        {
            return headerTemplate ??= (builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "mr-1 select-none");

                if (Header != null)
                {
                    builder.AddContent(2, Header);
                }
                else
                {
                    builder.AddContent(3, Label);
                }

                builder.CloseElement();
            });
        }
    }

    internal RenderFragment<Model> CellTemplate
    {
        get
        {
            return cellTemplate ??= (rowData => builder =>
            {
                var i = 0;
                builder.OpenElement(0, "td");

                var cls = VisibleFrom != null
                    ? $"hidden {VisibleFrom.Value.ToBreakpointCellClass()} "
                    : "";

                cls += CellClass ?? ClassNames("px-6 py-4 whitespace-nowrap text-sm text-gray-500", @class);

                builder.AddAttribute(1, "class", cls);

                if (compiledExpression != null)
                {
                    var value = compiledExpression(rowData);
                    if (Template != null)
                    {
                        builder.AddContent(2, Template, rowData);
                    }
                    else
                    {
                        var formattedValue = Formatter != null
                            ? Formatter(value)
                            : string.IsNullOrEmpty(Format)
                                ? value?.ToString()
                                : string.Format("{0:" + Format + "}", value);
                        builder.AddContent(3, formattedValue);
                    }
                }

                builder.CloseElement();
            });
        }
    }

    public static Type? GetMemberType(MemberInfo member) => member.MemberType switch
    {
        MemberTypes.Field => ((FieldInfo)member).FieldType,
        MemberTypes.Method => ((MethodInfo)member).ReturnType,
        MemberTypes.Property => ((PropertyInfo)member).PropertyType,
        _ => throw new NotSupportedException("MemberInfo must be FieldInfo, MethodInfo, or PropertyInfo not " + member.MemberType),
    };

    private static Type? GetMemberType<T>(Expression<T> expression) => expression.Body switch
    {
        MemberExpression m => GetMemberType(m.Member),
        UnaryExpression u when u.Operand is MemberExpression m => GetMemberType(m.Member),
        _ => null
    };

    private static string GetMemberName<T>(Expression<T> expression) => expression.Body switch
    {
        MemberExpression m => m.Member.Name,
        UnaryExpression u when u.Operand is MemberExpression m => m.Member.Name,
        _ => throw new NotSupportedException("Expression of type '" + expression.GetType().ToString() + "' is not supported")
    };
}
