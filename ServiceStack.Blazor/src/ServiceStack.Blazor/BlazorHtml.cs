using Microsoft.AspNetCore.Components;
using ServiceStack.Text;

namespace ServiceStack.Blazor;

public static class BlazorHtml
{
    internal static MarkupString NullJson = new("null");

    public static MarkupString Raw(string? text) => (MarkupString)(text ?? "");
    public static MarkupString RawJson<T>(T model)
    {
        var json = !Equals(model, default(T))
            ? model.ToJson()
            : null;
        return json != null ? (MarkupString)json : NullJson;
    }

    public static MarkupString ImportMap(Dictionary<string, (string Dev, string Prod)> importMaps)
    {
        var map = new Dictionary<string, object>();
        var imports = new Dictionary<string, object> { ["imports"] = map };
        var isDev = BlazorConfig.Instance.IsDevelopment;
        foreach (var importMap in importMaps)
        {
            map[importMap.Key] = isDev ? importMap.Value.Dev : importMap.Value.Prod;
        }
        var script = $"<script type=\"importmap\">\n{imports.ToJson().IndentJson()}\n</script>";
        return Raw(script);
    }

    public static MarkupString StaticImportMap(Dictionary<string, string> importMaps)
    {
        var to = new Dictionary<string, (string Dev, string Prod)>();
        foreach (var entry in importMaps)
        {
            to[entry.Key] = (entry.Value, entry.Value);
        }
        return ImportMap(to);
    }

    public static async Task<MarkupString> ApiAsJsonAsync<TResponse>(this IServiceGateway gateway, IReturn<TResponse> request)
    {
        return RawJson((await gateway.ApiAsync(request).ConfigAwait()).Response);
    }

    public static async Task<MarkupString> ApiResultsAsJsonAsync<T>(this IServiceGateway gateway, IReturn<QueryResponse<T>> request)
    {
        var api = await gateway.ApiAsync(request).ConfigAwait();
        return RawJson(api.Response?.Results);
    }
}