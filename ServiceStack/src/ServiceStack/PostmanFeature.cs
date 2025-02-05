using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.DataAnnotations;
using ServiceStack.Host;
using ServiceStack.Model;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack;

/// <summary>
/// Postman 2 Feature
/// </summary>
public class PostmanFeature : IPlugin, IHasStringId, IPreInitPlugin, IConfigureServices
{
    public string Id { get; set; } = Plugins.Postman;
    public string AtRestPath { get; set; }
    public bool? EnableSessionExport { get; set; }
    public string Headers { get; set; }
    public List<string> DefaultLabelFmt { get; set; }

    public readonly Dictionary<string, string> FriendlyTypeNames = new() {
        {"Int32", "int"},
        {"Int64", "long"},
        {"Boolean", "bool"},
        {"String", "string"},
        {"Double", "double"},
        {"Single", "float"},
    };

    /// <summary>
    /// Only generate specified Verb entries for "ANY" routes
    /// </summary>
    public List<string> DefaultVerbsForAny { get; set; }

    public PostmanFeature()
    {
        this.AtRestPath = "/postman";
        this.Headers = "Accept: " + MimeTypes.Json;
        this.DefaultVerbsForAny = [HttpMethods.Get];
        this.DefaultLabelFmt = ["type"];
    }

    public void BeforePluginsLoaded(IAppHost appHost)
    {
        appHost.ConfigurePlugin<MetadataFeature>(
            feature => feature.AddPluginLink(AtRestPath.TrimStart('/'), "Postman Metadata"));
    }

    public void Register(IAppHost appHost)
    {
        if (EnableSessionExport == null)
            EnableSessionExport = appHost.Config.DebugMode;
    }

    public void Configure(IServiceCollection services)
    {
        services.RegisterService(typeof(PostmanService), AtRestPath);
    }
}

[ExcludeMetadata]
public class Postman : IGet, IReturn<PostmanCollection>
{
    public List<string> Label { get; set; }
    public bool ExportSession { get; set; }
    public string ssid { get; set; }
    public string sspid { get; set; }
    public string ssopt { get; set; }
}

public class PostmanCollectionInfo
{
    public string name { get; set; }
    public string version { get; set; }
    public string schema { get; set; }
}

public class PostmanCollection
{
    public PostmanCollectionInfo info { get; set; } = new();
    public List<PostmanRequest> item { get; set; }
}

public class PostmanRequestBody
{
    public string mode { get; set; } = "formdata";
    public List<PostmanData> formdata { get; set; }
}

public class PostmanRequestUrl
{
    public string raw { get; set; }
    public string protocol { get; set; }
    public string host { get; set; }
    public string[] path { get; set; }
    public string port { get; set; }
    public List<PostmanRequestKeyValue> query { get; set; }
    public List<PostmanRequestKeyValue> variable { get; set; }
}

public class PostmanRequestDetails
{
    public PostmanRequestUrl url { get; set; }
    public string method { get; set; }
    public string header { get; set; }

    public PostmanRequestBody body { get; set; }
}

public class PostmanRequestKeyValue
{
    public string value { get; set; }
    public string key { get; set; }
}

public class PostmanRequest
{
    public string name { get; set; }
    public PostmanRequestDetails request { get; set; } = new();
}

public class PostmanData
{
    public string key { get; set; }
    public string value { get; set; }
    public string type { get; set; }
}

[DefaultRequest(typeof(Postman))]
[Restrict(VisibilityTo = RequestAttributes.None)]
public class PostmanService : Service
{
    [AddHeader(ContentType = MimeTypes.Json)]
    public object Any(Postman request)
    {
        var feature = HostContext.GetPlugin<PostmanFeature>();

        if (request.ExportSession)
        {
            if (feature.EnableSessionExport != true)
                throw new ArgumentException("PostmanFeature.EnableSessionExport is not enabled");

            var url = Request.GetBaseUrl()
                .CombineWith(Request.PathInfo)
                .AddQueryParam("ssopt", Request.GetItemOrCookie(SessionFeature.SessionOptionsKey))
                .AddQueryParam("sspid", Request.GetPermanentSessionId())
                .AddQueryParam("ssid", Request.GetTemporarySessionId());

            return HttpResult.Redirect(url);
        }

        var id = HostContext.AppHost.CreateSessionId();
        var ret = new PostmanCollection
        {
            info = new PostmanCollectionInfo
            {
                version = "1",
                name = HostContext.AppHost.ServiceName,
                schema = "https://schema.getpostman.com/json/collection/v2.0.0/collection.json"
            },
            item = GetRequests(request, id, HostContext.Metadata.OperationsMap.Values),
        };

        return ret;
    }

    public List<PostmanRequest> GetRequests(Postman request, string parentId, IEnumerable<Operation> operations)
    {
        var ret = new List<PostmanRequest>();
        var feature = HostContext.GetPlugin<PostmanFeature>();

        var headers = feature.Headers ?? ("Accept: " + MimeTypes.Json);

        if (Response is IHttpResponse httpRes)
        {
            if (request.ssopt != null
                || request.sspid != null
                || request.ssid != null)
            {
                if (feature.EnableSessionExport != true)
                    throw new ArgumentException("PostmanFeature.EnableSessionExport is not enabled");
            }

            if (request.ssopt != null)
            {
                Request.AddSessionOptions(request.ssopt);
            }
            if (request.sspid != null)
            {
                httpRes.Cookies.AddPermanentCookie(SessionFeature.PermanentSessionId, request.sspid);
            }
            if (request.ssid != null)
            {
                httpRes.Cookies.AddSessionCookie(SessionFeature.SessionId, request.ssid,
                    (HostContext.Config.UseSecureCookies && Request.IsSecureConnection));
            }
        }

        foreach (var op in operations)
        {
            Uri url = null;

            if (!HostContext.Metadata.IsVisible(base.Request, op))
                continue;

            var allVerbs = new HashSet<string>(op.Actions.Concat(
                    op.Routes.SelectMany(x => x.Verbs))
                .SelectMany(x => x == ActionContext.AnyAction
                    ? feature.DefaultVerbsForAny
                    : [x]));

            var propertyTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            op.RequestType.GetSerializableFields()
                .Each(x => propertyTypes[x.Name] = x.FieldType.AsFriendlyName(feature));
            op.RequestType.GetSerializableProperties()
                .Each(x => propertyTypes[x.Name] = x.PropertyType.AsFriendlyName(feature));

            foreach (var route in op.Routes)
            {
                var routeVerbs = route.Verbs.Contains(ActionContext.AnyAction)
                    ? feature.DefaultVerbsForAny.ToArray()
                    : route.Verbs;

                var restRoute = route.ToRestRoute();

                foreach (var verb in routeVerbs)
                {
                    allVerbs.Remove(verb); //exclude handled verbs

                    var routeData = restRoute.QueryStringVariables
                        .Map(x => new PostmanData
                        {
                            key = x,
                            value = "",
                            type = "text",
                        })
                        .ApplyPropertyTypes(propertyTypes);

                    url = new Uri(Request.GetBaseUrl().CombineWith(restRoute.Path.ToPostmanPathVariables()));
                    ret.Add(new PostmanRequest
                    {
                        request = new PostmanRequestDetails {
                            url = new PostmanRequestUrl {
                                raw = url.OriginalString,
                                host = url.Host,
                                port = url.Port.ToString(),
                                protocol = url.Scheme,
                                path = url.LocalPath.SplitPaths(),
                                query = !HttpUtils.HasRequestBody(verb) 
                                    ? routeData.Select(x => x.key)
                                        .ApplyPropertyTypes(propertyTypes)
                                        .Map(x => new PostmanRequestKeyValue { key = x.Key, value = x.Value }) 
                                    : null,
                                variable = restRoute.Variables.Any() 
                                    ? restRoute.Variables.Map(x => new PostmanRequestKeyValue { key = x }) 
                                    : null
                            },
                            method = verb,
                            body = new PostmanRequestBody {
                                formdata = HttpUtils.HasRequestBody(verb)
                                    ? routeData
                                    : null,
                            },
                            header = headers,
                        },
                        name = GetName(feature, request, op.RequestType, restRoute.Path),
                    });
                }
            }

            var emptyRequest = op.RequestType.CreateInstance();
            var virtualPath = emptyRequest.ToReplyUrlOnly();

            var requestParams = propertyTypes
                .Map(x => new PostmanData
                {
                    key = x.Key,
                    value = x.Value,
                    type = "text",
                });

            url = new Uri(Request.GetBaseUrl().CombineWith(virtualPath));

            ret.AddRange(allVerbs.Select(verb =>
                new PostmanRequest
                {
                    request = new PostmanRequestDetails {
                        url = new PostmanRequestUrl {
                            raw = url.OriginalString,
                            host = url.Host,
                            port = url.Port.ToString(),
                            protocol = url.Scheme,
                            path = url.LocalPath.SplitPaths(),
                            query = !HttpUtils.HasRequestBody(verb) 
                                ? requestParams.Select(x => x.key)
                                    .Where(x => !x.StartsWith(":"))
                                    .ApplyPropertyTypes(propertyTypes)
                                    .Map(x => new PostmanRequestKeyValue { key = x.Key, value = x.Value }) 
                                : null,
                            variable = url.Segments.Any(x => x.StartsWith(":")) 
                                ? url.Segments.Where(x => x.StartsWith(":"))
                                    .Map(x => new PostmanRequestKeyValue { key = x.Replace(":", ""), value = "" }) 
                                : null
                        },
                        method = verb,
                        body = new PostmanRequestBody {
                            formdata = HttpUtils.HasRequestBody(verb)
                                ? requestParams
                                : null,
                        },
                        header = headers,
                    },
                    name = GetName(feature, request, op.RequestType, virtualPath),
                }));
        }

        return ret;
    }

    public string GetName(PostmanFeature feature, Postman request, Type requestType, string virtualPath)
    {
        var fragments = request.Label ?? feature.DefaultLabelFmt;
        var sb = StringBuilderCache.Allocate();
        foreach (var fragment in fragments)
        {
            var parts = fragment.ToLower().Split(':');
            var asEnglish = parts.Length > 1 && parts[1] == "english";

            if (parts[0] == "type")
            {
                sb.Append(asEnglish ? requestType.Name.ToEnglish() : requestType.Name);
            }
            else if (parts[0] == "route")
            {
                sb.Append(virtualPath);
            }
            else
            {
                sb.Append(parts[0]);
            }
        }
        return StringBuilderCache.ReturnAndFree(sb);
    }
}

public static class PostmanExtensions
{
    private static readonly char[] PathDelim = ['/'];
    internal static string[] SplitPaths(this string text) => 
        text.Split(PathDelim, StringSplitOptions.RemoveEmptyEntries);

    public static string ToPostmanPathVariables(this string path)
    {
        return path.Replace("{", ":").Replace("}", "").TrimEnd('*');
    }

    public static string AsFriendlyName(this Type type, PostmanFeature feature)
    {
        var parts = type.Name.SplitOnFirst('`');
        var typeName = parts[0].LeftPart('[');
        var suffix = "";

        var nullableType = Nullable.GetUnderlyingType(type);
        if (nullableType != null)
        {
            typeName = nullableType.Name;
            suffix = "?";
        }
        else if (type.IsArray)
        {
            suffix = "[]";
        }
        else if (type.IsGenericType)
        {
            var args = type.GetGenericArguments().Map(x =>
                x.AsFriendlyName(feature));
            suffix = $"<{string.Join(",", args.ToArray())}>";
        }

        return feature.FriendlyTypeNames.TryGetValue(typeName, out var friendlyName)
            ? friendlyName + suffix
            : typeName + suffix;
    }

    public static List<PostmanData> ApplyPropertyTypes(this List<PostmanData> data,
        Dictionary<string, string> typeMap, string defaultValue = "")
    {
        data.Each(x => x.value = typeMap.TryGetValue(x.key, out var typeName) ? typeName : x.value ?? defaultValue);
        return data;
    }
        
    public static Dictionary<string, string> ApplyPropertyTypes(this IEnumerable<string> names,
        Dictionary<string, string> typeMap,
        string defaultValue = "")
    {
        var to = new Dictionary<string, string>();
        names.Each(x => to[x] = typeMap.TryGetValue(x, out var typeName) ? typeName : defaultValue);
        return to;
    }        
}