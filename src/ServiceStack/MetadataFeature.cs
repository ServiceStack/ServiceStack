using System.Web;
using ServiceStack.Web;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.WebHost.Endpoints.Metadata;

namespace ServiceStack
{
    public class MetadataFeature : IPlugin
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

        private IHttpHandler GetHandlerForPathParts(string[] pathParts)
        {
            var pathController = string.Intern(pathParts[0].ToLower());
            if (pathParts.Length == 1)
            {
                if (pathController == "metadata")
                    return new IndexMetadataHandler();

                return null;
            }

            var pathAction = string.Intern(pathParts[1].ToLower());
            if (pathAction == "wsdl")
            {
                if (pathController == "soap11")
                    return new Soap11WsdlMetadataHandler();
                if (pathController == "soap12")
                    return new Soap12WsdlMetadataHandler();
            }

            if (pathAction != "metadata") return null;

            switch (pathController)
            {
                case "json":
                    return new JsonMetadataHandler();

                case "xml":
                    return new XmlMetadataHandler();

                case "jsv":
                    return new JsvMetadataHandler();

                case "soap11":
                    return new Soap11MetadataHandler();

                case "soap12":
                    return new Soap12MetadataHandler();

                case "types":
                    
                    if (EndpointHost.Config == null
                        || EndpointHost.Config.MetadataTypesConfig == null)
                        return null;

                    if (EndpointHost.Config.MetadataTypesConfig.BaseUrl == null)
                        EndpointHost.Config.MetadataTypesConfig.BaseUrl = ServiceStackHttpHandlerFactory.GetBaseUrl();

                    return new MetadataTypesHandler { Config = EndpointHost.AppHost.Config.MetadataTypesConfig };

                case "operations":
                    
                    return new ActionHandler((httpReq, httpRes) => 
                        EndpointHost.Config.HasAccessToMetadata(httpReq, httpRes) 
                            ? EndpointHost.Metadata.GetOperationDtos()
                            : null, "Operations");

                default:
                    string contentType;
                    if (EndpointHost.ContentTypes
                        .ContentTypeFormats.TryGetValue(pathController, out contentType))
                    {
                        var format = ContentFormat.GetContentFormat(contentType);
                        return new CustomMetadataHandler(contentType, format);
                    }
                    break;
            }
            return null;
        }
    }
}