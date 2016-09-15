using System.Web;
using ServiceStack.Host.Handlers;

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
            var pathController = pathParts[0].ToLower();
            if (pathParts.Length == 1)
            {
#if !NETSTANDARD1_6
                if (pathController == "soap11")
                    return new Soap11MessageReplyHttpHandler();
                if (pathController == "soap12")
                    return new Soap12MessageReplyHttpHandler();
#endif

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
                    string contentType;
                    if (HostContext.ContentTypes.ContentTypeFormats.TryGetValue(pathController, out contentType))
                    {
                        var feature = contentType.ToFeature();
                        if (feature == Feature.None) feature = Feature.CustomFormat;

                        if (isReply)
                            return new GenericHandler(contentType, RequestAttributes.Reply, feature)
                            {
                                RequestName = requestName,
                            };
                        if (isOneWay)
                            return new GenericHandler(contentType, RequestAttributes.OneWay, feature)
                            {
                                RequestName = requestName,
                            };
                    }
                    break;
            }

            return null;
        }
    }
}