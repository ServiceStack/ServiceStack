#nullable enable
using System.Text;

namespace ServiceStack;

public static class CssUtils
{
    public static class Bootstrap
    {
        public static string InvalidClass<T>(ApiResult<T> apiResult, string fieldName) => apiResult.ErrorStatus.HasFieldError(fieldName)
            ? "is-invalid"
            : "";

        public static string InvalidClass(ResponseStatus? status, string fieldName) => status?.FieldError(fieldName) != null
            ? "is-invalid"
            : "";
    }
    
    public static string ClassNames(params string?[] classes)
    {
        var sb = new StringBuilder();
        foreach (var cls in classes)
        {
            if (string.IsNullOrEmpty(cls))
                continue;

            if (sb.Length > 0)
                sb.Append(' ');
            sb.Append(cls);
        }
        return sb.ToString();
    }
}