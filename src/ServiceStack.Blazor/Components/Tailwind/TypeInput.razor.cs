using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components.Tailwind;

public partial class TypeInput
{
    [Parameter, EditorRequired]
    public Dictionary<string, object> Model { get; set; } = new();
    string Value { get => Model.TryGetValue(propName, out var value) ? value?.ToString() ?? "" : ""; set => Model[propName] = value ?? ""; }

    [Parameter, EditorRequired]
    public MetadataPropertyType? Property { get; set; }

    string propName => Property!.Name;
    string propertyType => realType(Property!);
    [Parameter]
    public string Size { get; set; } = "md";
    string inputType => getInputType(Property!);
    string useHelp => propertyType != "Boolean" ? TextUtils.Humanize(propName) : "";
    List<KeyValuePair<string, string>> kvpValues() => TextUtils.ToKeyValuePairs((System.Collections.IEnumerable)Model);
    static string[] numberTypes = new[] { "SByte", "Byte", "Int16", "Int32", "Int64", "UInt16", "UInt32", "UInt64" };
    static string[] realTypes = new[] { "Single", "Double", "Decimal" };
    static string realType(MetadataPropertyType f) => f.Type == "Nullable`1" ? f.GenericArgs[0] : f.Type;
    static string getInputType(MetadataPropertyType propType)
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
