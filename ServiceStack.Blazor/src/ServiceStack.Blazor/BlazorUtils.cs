using ServiceStack.Text;

namespace ServiceStack.Blazor;

/// <summary>
/// Also extend functionality to any class implementing IHasJsonApiClient
/// </summary>
public static class BlazorUtils
{
    public static int nextId = 0;
    public static int NextId() => nextId++;

    public static void Log(string? message = null)
    {
        if (BlazorConfig.Instance.EnableVerboseLogging)
            Console.WriteLine(message ?? "");
    }

    public static string FormatValue(object? value)
    {
        if (value == null)
            return string.Empty;

        if (TextUtils.IsComplexType(value?.GetType()))
        {
            var str = TypeSerializer.Dump(value).TrimStart();
            if (str.Length < BlazorConfig.Instance.MaxFieldLength)
                return str;
            var to = TextUtils.Truncate(str, BlazorConfig.Instance.MaxFieldLength);
            if (to.StartsWith("{"))
                return to + "... }";
            else if (to.StartsWith("["))
                return to + "... ]";
            return to;
        }
        {
            var str = TextUtils.GetScalarText(value);
            return TextUtils.Truncate(str, BlazorConfig.Instance.MaxFieldLength);
        }
    }
}
