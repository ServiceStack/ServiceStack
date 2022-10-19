using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using System.Collections;
using ServiceStack.Blazor.Components.Tailwind;
using System.Data.Common;

namespace ServiceStack.Blazor.Components;

public class Column<Model> : UiComponentBase
{
    [Inject] public LocalStorage LocalStorage { get; set; }
    public int InstanceId = BlazorUtils.NextId();

    [CascadingParameter] public DataGrid<Model>? DataGrid { get; set; }
    [Parameter] public Expression<Func<Model, object>>? Field { get; set; }
    [Parameter] public string? FieldName { get; set; }
    [Parameter] public string? HeaderClass { get; set; }
    [Parameter] public Breakpoint? HeaderBreakpoint { get; set; }
    [Parameter] public string? CellClass { get; set; }
    [Parameter] public string? Title { get; set; }
    [Parameter] public string? Format { get; set; }
    [Parameter] public bool AllowFiltering { get; set; } = true;
    [Parameter] public Func<object, string>? Formatter { get; set; }
    [Parameter] public Breakpoint? VisibleFrom { get; set; }
    [Parameter] public ColumnSettings Settings { get; set; } = new();

    // Use the provided title or infer it from the expression
    public string? Label => Title ?? (Field != null ? GetMemberName(Field) : Property?.Name)?.SplitCamelCase().ToTitleCase();

    protected override void OnInitialized()
    {
        base.OnInitialized();
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
        ValidateCompiledExpression();
    }

    Func<Model, object>? ValidateCompiledExpression()
    {
        if (Field != null && lastCompiledExpression != Field)
        {
            compiledExpression = Field?.Compile();
            lastCompiledExpression = Field;
        }
        return compiledExpression;
    }

    public string CacheKey => $"Column/{DataGrid?.Id ?? "DataGrid"}:{typeof(Model).Name}.{Name}";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            Settings = await LocalStorage.GetItemAsync<ColumnSettings>(CacheKey) ?? new();
            await DataGrid!.NotifyPropertyChanged(nameof(Settings));
            await DataGrid!.FiltersChanged.InvokeAsync();
        }
    }

    public async Task SaveSettingsAsync()
    {
        await LocalStorage.SetItemAsync(CacheKey, Settings);
        await DataGrid!.NotifyPropertyChanged(nameof(Settings));
    }

    public async Task RemoveSettingsAsync()
    {
        Settings.Clear();
        await LocalStorage.RemoveAsync(CacheKey);
        await DataGrid!.NotifyPropertyChanged(nameof(Settings));
    }

    public string Name => Field != null ? GetMemberName(Field) : FieldName ?? Property?.Name ?? throw new Exception("Field or FieldName needs to be set");
    public Type FieldType => (Field != null ? GetMemberType(Field) : null) ?? Property?.PropertyType ?? typeof(object);
    public KeyValuePair<string, string>[] EnumEntries => FieldType.IsEnum
        ? Html.Input.GetEnumEntries(FieldType)
        : Array.Empty<KeyValuePair<string, string>>();

    public PropertyAccessor PropertyAccessor { get; set; }
    public PropertyInfo? Property => PropertyAccessor?.PropertyInfo;

    public MetadataPropertyType? MetadataProperty { get; set; }

    public bool IsComputed => PropertyAccessor != null ? TextUtils.IsComputed(PropertyAccessor.PropertyInfo) : MetadataProperty?.Attributes?.Any(x => x.Name == "Computed" || x.Name == "CustomSelect") == true;

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

    string? GetFormattedValue(object? value)
    {
        if (value == null) return "";
        var type = value.GetType();
        var formattedValue = Formatter != null
            ? Formatter(value)
            : string.IsNullOrEmpty(Format)
                ? value?.ToString()
                : string.Format("{0:" + Format + "}", value);
        return formattedValue != null
            ? TextUtils.Truncate(formattedValue, DataGrid!.MaxFieldLength)
            : null;
    }

    internal RenderFragment<Model> CellTemplate
    {
        get
        {
            return cellTemplate ??= (rowData => builder =>
            {
                builder.OpenElement(0, "td");

                var cls = VisibleFrom != null
                    ? $"hidden {VisibleFrom.Value.ToBreakpointCellClass()} "
                    : "";

                cls += CellClass ?? ClassNames(CssDefaults.Grid.TableCellClass, @class);

                builder.AddAttribute(1, "class", cls);

                var fieldExpr = ValidateCompiledExpression();
                object? value = fieldExpr != null
                    ? fieldExpr(rowData)
                    : PropertyAccessor != null
                        ? PropertyAccessor.PublicGetter(rowData)
                        : null;

                if (Template != null)
                {
                    builder.AddContent(2, Template, rowData);
                }
                else if (value != null)
                {
                    var format = MetadataProperty?.Format;
                    if (!TextUtils.IsComplexType(value.GetType()) && format == null)
                    {
                        builder.AddContent(3, GetFormattedValue(value));
                    }
                    else
                    {
                        builder.OpenComponent<PreviewFormat>(4);
                        builder.AddAttribute(5, "Value", value);
                        if (format != null)
                            builder.AddAttribute(6, "Format", format);
                        builder.CloseComponent();
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
