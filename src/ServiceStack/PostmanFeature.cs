using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Host;
using ServiceStack.Text;

namespace ServiceStack
{
    public class PostmanFeature : IPlugin
    {
        public string AtRestPath { get; set; }

        /// <summary>
        /// Only generate specified Verb entries for "ANY" routes
        /// </summary>
        public List<string> DefaultVerbsForAny { get; set; }

        public PostmanFeature()
        {
            this.AtRestPath = "/postman";
            this.DefaultVerbsForAny = new List<string> { HttpMethods.Get };
        }

        public void Register(IAppHost appHost)
        {
            appHost.RegisterService<PostmanService>(AtRestPath);
        }
    }

    public class Postman
    {
        public List<string> Label { get; set; }
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
    public class PostmanService : Service
    {
        [AddHeader(ContentType = MimeTypes.Json)]
        public object Any(Postman request)
        {
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
            decimal version;
            decimal.TryParse(HostContext.Config.ApiVersion.TrimStart('v'), out version);

            var ret = new List<PostmanRequest>();
            var feature = HostContext.GetPlugin<PostmanFeature>();

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

                foreach (var route in op.Routes)
                {
                    var routeVerbs = route.Verbs.Contains(ActionContext.AnyAction)
                        ? feature.DefaultVerbsForAny.ToArray()
                        : route.Verbs;

                    var restRoute = route.ToRestRoute();

                    foreach (var verb in routeVerbs)
                    {
                        allVerbs.Remove(verb); //exclude handled routes

                        ret.Add(new PostmanRequest
                        {
                            collectionId = parentId,
                            id = SessionExtensions.CreateRandomSessionId(),
                            method = verb,
                            url = Request.GetBaseUrl().CombineWith(restRoute.Path.ToPostmanPathVariables()),
                            name = GetName(request, op.RequestType, restRoute.Path),
                            description = op.RequestType.GetDescription(),
                            pathVariables = restRoute.Variables.ToDictionary(x => x),
                            data = restRoute.QueryStringVariables
                                .Map(x => new PostmanData
                                {
                                    key = x,
                                    value = "",
                                    type = "text",
                                }),
                            dataMode = "params",
                            headers = "Accept: " + MimeTypes.Json,
                            version = (int)version,
                            time = DateTime.UtcNow.ToUnixTimeMs(),
                        });
                    }
                }

                var emptyRequest = op.RequestType.CreateInstance();
                var virtualPath = emptyRequest.ToReplyUrlOnly();
                ret.AddRange(allVerbs.Select(verb =>
                    new PostmanRequest
                    {
                        collectionId = parentId,
                        id = SessionExtensions.CreateRandomSessionId(),
                        method = verb,
                        url = Request.GetBaseUrl().CombineWith(virtualPath),
                        name = GetName(request, op.RequestType, virtualPath),
                        description = op.RequestType.GetDescription(),
                        data = AutoMappingUtils.PopulateWith(emptyRequest)
                            .ToStringDictionary()
                            .Map(a => new PostmanData
                            {
                                key = a.Key,
                                value = a.Value,
                                type = "text",
                            }),
                        dataMode = "params",
                        headers = "Accept: " + MimeTypes.Json,
                        version = (int)version,
                        time = DateTime.UtcNow.ToUnixTimeMs(),
                    }));
            }

            return ret;
        }

        public string GetName(Postman request, Type requestType, string virtualPath)
        {
            var fragments = request.Label ?? new List<string> { "type" };
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
    }
}