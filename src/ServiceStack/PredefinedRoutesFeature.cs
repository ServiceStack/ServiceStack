using System.Web;
using ServiceStack.ServiceHost;
using ServiceStack.Web;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack
{
    public class PredefinedRoutesFeature : IPlugin
    {
        public void Register(IAppHost appHost)
        {
            appHost.CatchAllHandlers.Add(ProcessRequest);
        }

        public IHttpHandler ProcessRequest(string httpMethod, string pathInfo, string filePath)
        {
            var pathParts = pathInfo.TrimStart('/').Split('/');
            if (pathParts.Length == 0) return null;
            return GetHandlerForPathParts(pathParts);
        }

        private static IHttpHandler GetHandlerForPathParts(string[] pathParts)
        {
            var pathController = string.Intern(pathParts[0].ToLower());
            if (pathParts.Length == 1)
            {
                if (pathController == "soap11")
                    return new Soap11MessageSyncReplyHttpHandler();
                if (pathController == "soap12")
                    return new Soap12MessageSyncReplyHttpHandler();

                return null;
            }

            var pathAction = string.Intern(pathParts[1].ToLower());
            var requestName = pathParts.Length > 2 ? pathParts[2] : null;
            var isReply = pathAction == "reply";
            var isOneWay = pathAction == "oneway";
            switch (pathController)
            {
                case "json":
                    if (isReply)
                        return new JsonSyncReplyHandler { RequestName = requestName };
                    if (isOneWay)
                        return new JsonAsyncOneWayHandler { RequestName = requestName };
                    break;

                case "xml":
                    if (isReply)
                        return new XmlSyncReplyHandler { RequestName = requestName };
                    if (isOneWay)
                        return new XmlAsyncOneWayHandler { RequestName = requestName };
                    break;

                case "jsv":
                    if (isReply)
                        return new JsvSyncReplyHandler { RequestName = requestName };
                    if (isOneWay)
                        return new JsvAsyncOneWayHandler { RequestName = requestName };
                    break;

                default:
                    string contentType;
                    if (EndpointHost.ContentTypes.ContentTypeFormats.TryGetValue(pathController, out contentType))
                    {
                        var feature = ContentFormat.ToFeature(contentType);
                        if (feature == Feature.None) feature = Feature.CustomFormat;

                        if (isReply)
                            return new GenericHandler(contentType, EndpointAttributes.Reply, feature) {
                                RequestName = requestName
                            };
                        if (isOneWay)
                            return new GenericHandler(contentType, EndpointAttributes.OneWay, feature) {
                                RequestName = requestName
                            };
                    }
                    break;
            }

            return null;
        }
    }
}