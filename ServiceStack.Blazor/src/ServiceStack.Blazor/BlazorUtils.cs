using ServiceStack.Text;

namespace ServiceStack.Blazor;

/// <summary>
/// Also extend functionality to any class implementing IHasJsonApiClient
/// </summary>
public static class BlazorUtils
{
    public static int nextId = 0;
    public static int NextId() => nextId++;

    public static void LogError(string? message = null)
    {
        if (BlazorConfig.Instance.EnableErrorLogging)
            Console.WriteLine("ERROR: " + message ?? "");
    }

    public static void Log(string? message = null)
    {
        if (BlazorConfig.Instance.EnableLogging)
            Console.WriteLine(message ?? "");
    }

    public static void LogDebug(string? message = null)
    {
        if (BlazorConfig.Instance.EnableVerboseLogging)
            Console.WriteLine(message ?? "");
    }

    public static string FormatValue(object? value) => 
        FormatValue(value, BlazorConfig.Instance.MaxFieldLength, BlazorConfig.Instance.MaxNestedFieldLength);
    public static string FormatValue(object? value, int maxFieldLength, int maxNestedFieldLength)
    {
        if (value == null)
            return string.Empty;

        if (TextUtils.IsComplexType(value?.GetType()))
        {
            var str = TypeSerializer.Dump(value).TrimStart();
            if (str.Length < maxNestedFieldLength)
                return str;
            var to = TextUtils.Truncate(str, maxNestedFieldLength);
            if (to.StartsWith("{"))
                return to + "... }";
            else if (to.StartsWith("["))
                return to + "... ]";
            return to;
        }
        var s = TextUtils.GetScalarText(value);
        return TextUtils.Truncate(s, maxFieldLength);
    }

    public static string FormatValueAsHtml(object? Value)
    {
        string wrap(string raw, string html) => $"<span title=\"{raw.HtmlEncode()}\">" + html + "</span>";

        var sb = StringBuilderCache.Allocate();
        if (Value is System.Collections.IEnumerable e)
        {
            var first = TextUtils.FirstOrDefault(e);
            if (first == null)
                return "[]";

            if (TextUtils.IsComplexType(first.GetType()))
                return wrap(TypeSerializer.Dump(Value).HtmlEncode(), FormatValue(Value));

            foreach (var item in e)
            {
                if (sb.Length > 0)
                    sb.Append(',');
                sb.Append(TextUtils.GetScalarText(item));
            }

        }
        var dict = Value.ToObjectDictionary();
        var keys = dict.Keys.ToList();
        var len = Math.Min(BlazorConfig.Instance.MaxNestedFields, keys.Count);
        for (var i = 0; i < len; i++)
        {
            var key = keys[i];
            var val = dict[key];
            var value = FormatValue(val, BlazorConfig.Instance.MaxFieldLength, BlazorConfig.Instance.MaxNestedFieldLength);
            var str = TextUtils.Truncate(value, BlazorConfig.Instance.MaxNestedFieldLength).HtmlEncode();
            if (sb.Length > 0)
                sb.Append(", ");

            sb.AppendLine($"<b class=\"font-medium\">{key}</b>: {str}");
        }
        if (keys.Count > len)
            sb.AppendLine("...");

        var html = StringBuilderCache.ReturnAndFree(sb);
        return wrap(TypeSerializer.Dump(Value).HtmlEncode(), "{ " + html + " }");
    }

    public static bool SupportsProperty(MetadataPropertyType? prop)
    {
        if (prop?.Type == null) 
            return false;
        if (prop.IsValueType == true || prop.IsEnum == true)
            return true;

        var unwrapType = prop.Type.EndsWith('?')
            ? prop.Type[..^1]
            : prop.Type;

        return Html.Input.TypeNameMap.ContainsKey(unwrapType);
    }
}
