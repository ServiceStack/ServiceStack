using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components;

public class TypeInputBase : ApiComponentBase
{
    [Parameter, EditorRequired]
    public Dictionary<string, object> Model { get; set; } = new();
    protected string Value { get => Model.TryGetValue(PropName, out var value) ? value?.ToString() ?? "" : ""; set => Model[PropName] = value ?? ""; }

    [Parameter, EditorRequired]
    public MetadataPropertyType? Property { get; set; }

    protected string PropName => Property!.Name;
    protected string PropertyType => realType(Property!);
    [Parameter]
    public string Size { get; set; } = "md";
    protected string InputType => getInputType(Property!);
    protected string UseHelp => PropertyType != "Boolean" ? TextUtils.Humanize(PropName) : "";
    protected List<KeyValuePair<string, string>> KvpValues() => TextUtils.ToKeyValuePairs(Model);

    public static string[] numberTypes = new[] { "SByte", "Byte", "Int16", "Int32", "Int64", "UInt16", "UInt32", "UInt64" };
    public static string[] realTypes = new[] { "Single", "Double", "Decimal" };

    public static string realType(MetadataPropertyType f) => f.Type == "Nullable`1" ? f.GenericArgs[0] : f.Type;
    public static string getInputType(MetadataPropertyType propType)
    {
        var t = realType(propType);
        var name = propType.Name;
        if (t == nameof(Boolean))
            return "checkbox";
        if (Array.IndexOf(numberTypes, t) >= 0 || Array.IndexOf(realTypes, t) >= 0)
            return "number";
        if (t == nameof(DateTime) || t == nameof(DateTimeOffset))
            return "datetime-local";
        if (propType.IsEnum == true && propType.AllowableValues?.Length > 0)
            return "select";
        if (name != null)
        {
            if (name.EndsWith("Password"))
                return "password";
            if (name.EndsWith("Email"))
                return "email";
            if (name.EndsWith("Url"))
                return "url";
            if (name.IndexOf("Phone") >= 0)
                return "tel";
        }

        if (propType.IsValueType != true && t != nameof(String))
            return "textarea";
        return "text";
    }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public IReadOnlyDictionary<string, object>? IncludeAttributes => TextInputBase.SanitizeAttributes(AdditionalAttributes);
}
