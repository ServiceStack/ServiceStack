using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.DataAnnotations;
using ServiceStack.Host;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    public class PostmanFeature : IPlugin
    {
        public string AtRestPath { get; set; }
        public bool? EnableSessionExport { get; set; }
        public string Headers { get; set; }
        public List<string> DefaultLabelFmt { get; set; }

        public Dictionary<string, string> FriendlyTypeNames = new Dictionary<string, string>
        {
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
            this.DefaultVerbsForAny = new List<string> { HttpMethods.Get };
            this.DefaultLabelFmt = new List<string> { "type" };
        }

        public void Register(IAppHost appHost)
        {
            appHost.RegisterService<PostmanService>(AtRestPath);

            appHost.GetPlugin<MetadataFeature>()
                   .AddPluginLink(AtRestPath.TrimStart('/'), "Postman Metadata");

            if (EnableSessionExport == null)
                EnableSessionExport = appHost.Config.DebugMode;
        }
    }

    [Exclude(Feature.Soap)]
    public class Postman
    {
        public List<string> Label { get; set; }
        public bool ExportSession { get; set; }
        public string ssid { get; set; }
        public string sspid { get; set; }
        public string ssopt { get; set; }
    }

    public class PostmanCollection
    {
        public string id { get; set; }
        public string name { get; set; }
        public long timestamp { get; set; }
        public List<PostmanRequest> requests { get; set; }
    }

    public class PostmanRequest
    {
        public string collectionId { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string url { get; set; }
        public Dictionary<string, string> pathVariables { get; set; }
        public string method { get; set; }
        public string headers { get; set; }
        public string dataMode { get; set; }
        public long time { get; set; }
        public int version { get; set; }
        public List<PostmanData> data { get; set; }
        public List<string> responses { get; set; }
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

                var url = Request.ResolveBaseUrl()
                    .CombineWith(Request.PathInfo)
                    .AddQueryParam("ssopt", Request.GetItemOrCookie(SessionFeature.SessionOptionsKey))
                    .AddQueryParam("sspid", Request.GetPermanentSessionId())
                    .AddQueryParam("ssid", Request.GetTemporarySessionId());

                return HttpResult.Redirect(url);
            }

            var id = SessionExtensions.CreateRandomSessionId();
            var ret = new PostmanCollection
            {
                id = id,
                name = HostContext.AppHost.ServiceName,
                timestamp = DateTime.UtcNow.ToUnixTimeMs(),
                requests = GetRequests(request, id, HostContext.Metadata.OperationsMap.Values),
            };

            return ret;
        }

        public List<PostmanRequest> GetRequests(Postman request, string parentId, IEnumerable<Operation> operations)
        {
            var ret = new List<PostmanRequest>();
            var feature = HostContext.GetPlugin<PostmanFeature>();

            var headers = feature.Headers ?? ("Accept: " + MimeTypes.Json);

            var httpRes = Response as IHttpResponse;
            if (httpRes != null)
            {
                if (request.ssopt != null
                    || request.sspid != null
                    || request.ssid != null)
                {
                    if (feature.EnableSessionExport != true)
                    {
                        throw new ArgumentException("PostmanFeature.EnableSessionExport is not enabled");
                    }
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
                        (HostContext.Config.OnlySendSessionCookiesSecurely && Request.IsSecureConnection));
                }
            }

            foreach (var op in operations)
            {
                if (!HostContext.Metadata.IsVisible(base.Request, op))
                    continue;

                var allVerbs = op.Actions.Concat(
                    op.Routes.SelectMany(x => x.Verbs))
                        .SelectMany(x => x == ActionContext.AnyAction
                        ? feature.DefaultVerbsForAny
                        : new List<string> { x })
                    .ToHashSet();

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

                        ret.Add(new PostmanRequest
                        {
                            collectionId = parentId,
                            id = SessionExtensions.CreateRandomSessionId(),
                            method = verb,
                            url = Request.GetBaseUrl().CombineWith(restRoute.Path.ToPostmanPathVariables()),
                            name = GetName(feature, request, op.RequestType, restRoute.Path),
                            description = op.RequestType.GetDescription(),
                            pathVariables = !verb.HasRequestBody()
                                ? restRoute.Variables.Concat(routeData.Select(x => x.key))
                                    .ApplyPropertyTypes(propertyTypes)
                                : null,
                            data = verb.HasRequestBody()
                                ? routeData
                                : null,
                            dataMode = "params",
                            headers = headers,
                            version = 2,
                            time = DateTime.UtcNow.ToUnixTimeMs(),
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

                ret.AddRange(allVerbs.Select(verb =>
                    new PostmanRequest
                    {
                        collectionId = parentId,
                        id = SessionExtensions.CreateRandomSessionId(),
                        method = verb,
                        url = Request.GetBaseUrl().CombineWith(virtualPath),
                        pathVariables = !verb.HasRequestBody()
                            ? requestParams.Select(x => x.key)
                                .ApplyPropertyTypes(propertyTypes)
                            : null,
                        name = GetName(feature, request, op.RequestType, virtualPath),
                        description = op.RequestType.GetDescription(),
                        data = verb.HasRequestBody()
                            ? requestParams
                            : null,
                        dataMode = "params",
                        headers = headers,
                        version = 2,
                        time = DateTime.UtcNow.ToUnixTimeMs(),
                    }));
            }

            return ret;
        }

        public string GetName(PostmanFeature feature, Postman request, Type requestType, string virtualPath)
        {
            var fragments = request.Label ?? feature.DefaultLabelFmt;
            var sb = new StringBuilder();
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
            return sb.ToString();
        }
    }

    public static class PostmanExtensions
    {
        public static string ToPostmanPathVariables(this string path)
        {
            return path.Replace("{", ":").Replace("}", "").TrimEnd('*');
        }

        public static string AsFriendlyName(this Type type, PostmanFeature feature)
        {
            var parts = type.Name.SplitOnFirst('`');
            var typeName = parts[0].SplitOnFirst('[')[0];
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
            else if (type.IsGenericType())
            {
                var args = type.GetGenericArguments().Map(x => 
                    x.AsFriendlyName(feature));
                suffix = "<{0}>".Fmt(string.Join(",", args.ToArray()));
            }

            string frindlyName;
            return feature.FriendlyTypeNames.TryGetValue(typeName, out frindlyName)
                ? frindlyName + suffix
                : typeName + suffix;
        }

        public static List<PostmanData> ApplyPropertyTypes(this List<PostmanData> data,
            Dictionary<string, string> typeMap, string defaultValue = "")
        {
            string typeName;
            data.Each(x => x.value = typeMap.TryGetValue(x.key, out typeName) ? typeName : x.value ?? defaultValue);
            return data;
        }

        public static Dictionary<string, string> ApplyPropertyTypes(this IEnumerable<string> names,
            Dictionary<string, string> typeMap,
            string defaultValue = "")
        {
            var to = new Dictionary<string, string>();
            string typeName;
            names.Each(x => to[x] = typeMap.TryGetValue(x, out typeName) ? typeName : defaultValue);
            return to;
        }
    }
}