#nullable enable
using System.Text;

namespace ServiceStack;

public static class CssUtils
{
    public static class Bootstrap
    {
        public static string InputClass<T>(ApiResult<T> apiResult, string fieldName, string? valid = null, string? invalid = null) =>
            InputClass(apiResult.Error, fieldName, valid, invalid);

        public static string InputClass(ResponseStatus? status, string fieldName,
            string? valid = null,
            string? invalid = null)
            => status?.FieldError(fieldName) == null
                ? valid ?? ""
                : invalid ?? "is-invalid";
    }

    public static class Tailwind
    {
        public static string InputClass<T>(ApiResult<T> apiResult, string fieldName, string? valid = null, string? invalid = null) =>
            InputClass(apiResult.Error, fieldName, valid, invalid);

        public static string InputClass(ResponseStatus? status, string fieldName,
            string? valid = null,
            string? invalid = null)
            => status?.FieldError(fieldName) == null
                ? valid ?? "focus-within:ring-indigo-600 border-gray-300 focus-within:border-indigo-600"
                : invalid ?? "border-red-300 text-red-900 placeholder-red-300 focus:outline-none focus:ring-red-500 focus:border-red-500";
        
        public static string Input(string cls) => Input(false, cls);
        public static string Input(bool invalid, string cls)
        {
            return string.Join(" ", new string[] {
                "block w-full sm:text-sm rounded-md disabled:bg-gray-100 disabled:shadow-none", !invalid
                    ? "shadow-sm focus:ring-indigo-500 focus:border-indigo-500 border-gray-300"
                    : "pr-10 border-red-300 text-red-900 placeholder-red-300 focus:outline-none focus:ring-red-500 focus:border-red-500", cls });
        }
    }

    public static string Selected(bool condition) => condition ? "selected" : "";
    public static string Active(bool condition) => condition ? "active" : "";
    
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