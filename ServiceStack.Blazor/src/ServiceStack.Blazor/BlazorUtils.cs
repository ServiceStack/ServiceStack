using Microsoft.Extensions.Logging;
using ServiceStack.Text;
using System.Diagnostics;
using System.Security.AccessControl;

namespace ServiceStack.Blazor;

/// <summary>
/// Also extend functionality to any class implementing IHasJsonApiClient
/// </summary>
public static class BlazorUtils
{
    public static int nextId = 0;
    public static int NextId() => nextId++;

    public static void LogError(string message, params object[] args)
    {
        if (BlazorConfig.Instance.EnableErrorLogging)
            BlazorConfig.Instance.GetLog()?.LogError(message, args);
    }

    public static void LogError(Exception ex, string message, params object[] args)
    {
        if (BlazorConfig.Instance.EnableErrorLogging)
            BlazorConfig.Instance.GetLog()?.LogError(ex, message, args);
    }

    public static void Log(string message)
    {
        if (BlazorConfig.Instance.EnableLogging)
            BlazorConfig.Instance.GetLog()?.LogInformation(message);
    }

    public static void Log(string message, params object[] args)
    {
        if (BlazorConfig.Instance.EnableLogging)
            BlazorConfig.Instance.GetLog()?.LogInformation(message, args);
    }

    public static void LogDebug(string message)
    {
        if (BlazorConfig.Instance.EnableVerboseLogging)
            BlazorConfig.Instance.GetLog()?.LogDebug(message);
    }

    public static void LogDebug(string message, params object[] args)
    {
        if (BlazorConfig.Instance.EnableVerboseLogging)
            BlazorConfig.Instance.GetLog()?.LogDebug(message, args);
    }

    public static string FormatValue(object? value) => 
        FormatValue(value, BlazorConfig.Instance.MaxFieldLength);
    public static string FormatValue(object? value, int maxFieldLength)
    {
        if (value == null)
            return string.Empty;

        if (TextUtils.IsComplexType(value?.GetType()))
        {
            TextUtils.Dump(value);
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
                return wrap(TextUtils.FormatJson(Value).HtmlEncode(), FormatValue(Value));

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
            var value = FormatValue(val, BlazorConfig.Instance.MaxFieldLength);
            var str = TextUtils.Truncate(value, BlazorConfig.Instance.MaxNestedFieldLength).HtmlEncode();
            if (sb.Length > 0)
                sb.Append(", ");

            sb.AppendLine($"<b class=\"font-medium\">{key}</b>: {str}");
        }
        if (keys.Count > len)
            sb.AppendLine("...");

        var html = StringBuilderCache.ReturnAndFree(sb);
        return wrap(TextUtils.FormatJson(Value).HtmlEncode(), "{ " + html + " }");
    }

    public static bool SupportsProperty(MetadataPropertyType? prop)
    {
        if (prop?.Type == null) 
            return false;
        if (prop.IsValueType == true || prop.IsEnum == true)
            return true;
        if (prop.Input?.Type == Html.Input.Types.File)
            return true;
        if (prop.Input?.Type == Html.Input.Types.Tag)
            return true;

        var unwrapType = prop.Type.EndsWith('?')
            ? prop.Type[..^1]
            : prop.Type;

        return Html.Input.TypeNameMap.ContainsKey(unwrapType);
    }

    public static async Task OnApiErrorAsync(object requestDto, IHasErrorStatus apiError)
    {
        if (BlazorConfig.Instance.OnApiErrorAsync != null)
            await BlazorConfig.Instance.OnApiErrorAsync(requestDto, apiError);
    }

    public static async Task<ApiResult<TResponse>> ManagedApiAsync<TResponse>(this IServiceGateway client, IReturn<TResponse> request)
    {
        Stopwatch? sw = null;
        var config = BlazorConfig.Instance;
        var log = BlazorConfig.Instance.GetLog();
        if (config.EnableLogging)
        {
            sw = Stopwatch.StartNew();
            log?.LogDebug("API {0}", request.GetType().Name);
        }

        var ret = await client!.ApiAsync(request);
        if (ret.Error != null)
        {
            if (config.EnableErrorLogging)
            {
                log?.LogError("ERROR {0}:\n{1}", request.GetType().Name, ret.Error.GetDetailedError());
            }
            await OnApiErrorAsync(request, ret);
        }

        if (config.EnableLogging)
        {
            log?.LogDebug("END {0} took {1}ms", request.GetType().Name, sw!.ElapsedMilliseconds);
        }
        return ret;
    }

    public static async Task<ApiResult<EmptyResponse>> ManagedApiAsync(this IServiceGateway client, IReturnVoid request)
    {
        Stopwatch? sw = null;
        var config = BlazorConfig.Instance;
        var log = BlazorConfig.Instance.GetLog();
        if (config.EnableLogging)
        {
            sw = Stopwatch.StartNew();
            log?.LogDebug("API void {0}", request.GetType().Name);
        }

        var ret = await client.ApiAsync(request);
        if (ret.Error != null)
        {
            if (BlazorConfig.Instance.EnableErrorLogging)
            {
                log?.LogError("ERROR {0}:\n{1}", request.GetType().Name, ret.Error.GetDetailedError());
            }
            await OnApiErrorAsync(request, ret);
        }

        if (config.EnableLogging)
        {
            log?.LogDebug("END void {0} took {1}ms", request.GetType().Name, sw!.ElapsedMilliseconds);
        }
        return ret;
    }

    public static async Task<IHasErrorStatus> ManagedApiAsync<Model>(this IServiceGateway client, object request)
    {
        Stopwatch? sw = null;
        var config = BlazorConfig.Instance;
        var log = BlazorConfig.Instance.GetLog();
        if (config.EnableLogging)
        {
            sw = Stopwatch.StartNew();
            log?.LogDebug("API object {0}", request.GetType().Name);
        }

        var ret = await client!.ApiAsync<Model>(request);
        if (ret.Error != null)
        {
            if (BlazorConfig.Instance.EnableErrorLogging)
            {
                log?.LogError("ERROR {0}:\n{1}", request.GetType().Name, ret.Error.GetDetailedError());
            }
            await OnApiErrorAsync(request, ret);
        }

        if (config.EnableLogging)
        {
            log?.LogDebug("END object {0} took {1}ms", request.GetType().Name, sw!.ElapsedMilliseconds);
        }
        return ret;
    }

    public static async Task<ApiResult<Model>> ManagedApiFormAsync<Model>(this IServiceGatewayFormAsync client, object requestDto, MultipartFormDataContent formData)
    {
        Stopwatch? sw = null;
        var config = BlazorConfig.Instance;
        var log = BlazorConfig.Instance.GetLog();
        if (config.EnableLogging)
        {
            sw = Stopwatch.StartNew();
            log?.LogDebug("API Form {0}", formData.GetType().Name);
        }

        var ret = await client.ApiFormAsync<Model>(requestDto, formData);
        if (ret.Error != null)
        {
            if (BlazorConfig.Instance.EnableErrorLogging)
            {
                log?.LogError("ERROR {0}:\n{1}", formData.GetType().Name, ret.Error.GetDetailedError());
            }
            await OnApiErrorAsync(formData, ret);
        }

        if (config.EnableLogging)
        {
            log?.LogDebug("END Form {0} took {1}ms", formData.GetType().Name, sw!.ElapsedMilliseconds);
        }
        return ret;
    }

    public static async Task<ApiResult<AppMetadata>> ApiAppMetadataAsync(this IServiceGateway client)
    {
        Stopwatch? sw = null;
        var config = BlazorConfig.Instance;
        var log = BlazorConfig.Instance.GetLog();
        if (config.EnableLogging)
        {
            sw = Stopwatch.StartNew();
            log?.LogDebug("API ApiAppMetadataAsync");
        }

        var request = new MetadataApp();
        var ret = await client!.ApiCacheAsync(request);
        if (ret.Error != null)
        {
            if (BlazorConfig.Instance.EnableErrorLogging)
            {
                log?.LogError("ERROR AppMetadata:\n{0}", ret.Error.GetDetailedError());
            }
            await OnApiErrorAsync(request, ret);
        }

        if (config.EnableLogging)
        {
            log?.LogDebug("END ApiAppMetadataAsync took {0}ms", sw!.ElapsedMilliseconds);
        }
        return ret;
    }

    public static async Task<TResponse> ManagedSendAsync<TResponse>(this IServiceGateway client, IReturn<TResponse> request)
    {
        Stopwatch? sw = null;
        var config = BlazorConfig.Instance;
        var log = BlazorConfig.Instance.GetLog();
        if (config.EnableLogging)
        {
            sw = Stopwatch.StartNew();
            log?.LogDebug("API SendAsync {0}", request.GetType().Name);
        }

        try
        {
            var ret = await client!.SendAsync(request);
            if (config.EnableLogging)
            {
                log?.LogDebug("END SendAsync {0} took {1}ms", request.GetType().Name, sw!.ElapsedMilliseconds);
            }
            return ret;
        }
        catch (Exception e)
        {
            if (BlazorConfig.Instance.EnableErrorLogging)
            {
                var status = e.GetResponseStatus();
                log?.LogError("ERROR {0}:\n{1}", request.GetType().Name, status != null ? status.GetDetailedError() : e.ToString());
            }
            throw;
        }
    }
}
