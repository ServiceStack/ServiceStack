using System.Web;
using ServiceStack.Host.Handlers;

namespace ServiceStack
{
    public class RequestInfoFeature : IPlugin
    {
        public void Register(IAppHost appHost)
        {
            appHost.CatchAllHandlers.Add(ProcessRequest);

            appHost.GetPlugin<MetadataFeature>()
                .AddDebugLink("?{0}={1}".Fmt(Keywords.Debug, Keywords.RequestInfo), "Request Info");
        }

        public IHttpHandler ProcessRequest(string httpMethod, string pathInfo, string filePath)
        {
            var pathParts = pathInfo.TrimStart('/').Split('/');
            return pathParts.Length == 0 ? null : GetHandlerForPathParts(pathParts);
        }

        private static IHttpHandler GetHandlerForPathParts(string[] pathParts)
        {
            var pathController = pathParts[0].ToLower();
            return pathController == Keywords.RequestInfo
                ? new RequestInfoHandler()
                : null;
        }
    }
}