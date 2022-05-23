using System;
using System.Collections.Generic;
using ServiceStack.Host.Handlers;
#if NETFX
using System.Web;
#else
using ServiceStack.Host;
#endif

namespace ServiceStack
{
    public class PredefinedRoutesFeature : IPlugin, Model.IHasStringId
    {
        public string Id { get; set; } = Plugins.PredefinedRoutes;
        public Dictionary<string, Func<IHttpHandler>> HandlerMappings { get; } = new();

        public string JsonApiRoute { get; set; } = "/api/{Request}";
        
        public void Register(IAppHost appHost)
        {
            if (appHost.PathBase == null && JsonApiRoute != null)
            {
                appHost.RawHttpHandlers.Add(ApiHandlers.Json(JsonApiRoute));
                appHost.AddToAppMetadata(metadata => metadata.HttpHandlers["ApiHandlers.Json"] = JsonApiRoute);
            }
            
            appHost.CatchAllHandlers.Add(ProcessRequest);
        }

        public IHttpHandler ProcessRequest(string httpMethod, string pathInfo, string filePath)
        {
            var pathParts = pathInfo.TrimStart('/').Split('/');
            if (pathParts.Length == 0) return null;
            return GetHandlerForPathParts(pathParts);
        }

        private IHttpHandler GetHandlerForPathParts(string[] pathParts)
        {
            var pathController = pathParts[0].ToLower();

            if (pathParts.Length == 1)
            {
                if (HandlerMappings.TryGetValue(pathController, out var handlerFn))
                    return handlerFn();

                return null;
            }

            var pathAction = pathParts[1].ToLower();
            var requestName = pathParts.Length > 2 ? pathParts[2] : null;
            var isReply = pathAction == "reply";
            var isOneWay = pathAction == "oneway";
            switch (pathController)
            {
                case "json":
                    if (isReply)
                        return new JsonReplyHandler { RequestName = requestName };
                    if (isOneWay)
                        return new JsonOneWayHandler { RequestName = requestName };
                    break;

                case "xml":
                    if (isReply)
                        return new XmlReplyHandler { RequestName = requestName };
                    if (isOneWay)
                        return new XmlOneWayHandler { RequestName = requestName };
                    break;

                case "jsv":
                    if (isReply)
                        return new JsvReplyHandler { RequestName = requestName };
                    if (isOneWay)
                        return new JsvOneWayHandler { RequestName = requestName };
                    break;

                default:
                    if (HostContext.ContentTypes.ContentTypeFormats.TryGetValue(pathController, out var contentType))
                    {
                        var feature = contentType.ToFeature();
                        if (feature == Feature.None) feature = Feature.CustomFormat;

                        if (isReply)
                            return new GenericHandler(contentType, RequestAttributes.Reply, feature) {
                                RequestName = requestName,
                            };
                        if (isOneWay)
                            return new GenericHandler(contentType, RequestAttributes.OneWay, feature) {
                                RequestName = requestName,
                            };
                    }
                    break;
            }

            return null;
        }
    }
}