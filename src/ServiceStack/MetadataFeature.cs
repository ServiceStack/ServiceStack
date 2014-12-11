using System;
using System.Collections.Generic;
using System.Web;
using ServiceStack.Host.Handlers;
using ServiceStack.Metadata;

namespace ServiceStack
{
    public class MetadataFeature : IPlugin
    {
        public string PluginLinksTitle { get; set; }
        public Dictionary<string, string> PluginLinks { get; set; }

        public string DebugLinksTitle { get; set; }
        public Dictionary<string, string> DebugLinks { get; set; }

        public Action<IndexOperationsControl> IndexPageFilter { get; set; }
        public Action<OperationControl> DetailPageFilter { get; set; }


        public MetadataFeature()
        {

            PluginLinksTitle = "Plugin Links:";
            PluginLinks = new Dictionary<string, string>();

            DebugLinksTitle = "Debug Info:";
            DebugLinks = new Dictionary<string, string> {
                {"operations/metadata", "Operations Metadata"},
            };
        }

        public void Register(IAppHost appHost)
        {
            appHost.CatchAllHandlers.Add(ProcessRequest);
        }

        public virtual IHttpHandler ProcessRequest(string httpMethod, string pathInfo, string filePath)
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

                case "operations":
                    return new CustomResponseHandler((httpReq, httpRes) => 
                        HostContext.AppHost.HasAccessToMetadata(httpReq, httpRes) 
                            ? HostContext.Metadata.GetOperationDtos()
                            : null, "Operations");

                default:
                    string contentType;
                    if (HostContext.ContentTypes
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

    public static class MetadataFeatureExtensions
    {
        public static MetadataFeature AddPluginLink(this MetadataFeature metadata, string href, string title)
        {
            if (metadata != null)
            {
                if (HostContext.Config.HandlerFactoryPath != null && href[0] == '/')
                    href = "/" + HostContext.Config.HandlerFactoryPath + href;

                metadata.PluginLinks[href] = title;
            }
            return metadata;
        }

        public static MetadataFeature RemovePluginLink(this MetadataFeature metadata, string href)
        {
            metadata.PluginLinks.Remove(href);
            return metadata;
        }

        public static MetadataFeature AddDebugLink(this MetadataFeature metadata, string href, string title)
        {
            if (metadata != null)
            {
                if (HostContext.Config.HandlerFactoryPath != null && href[0] == '/')
                    href = "/" + HostContext.Config.HandlerFactoryPath + href;

                metadata.DebugLinks[href] = title;
            }
            return metadata;
        }

        public static MetadataFeature RemoveDebugLink(this MetadataFeature metadata, string href)
        {
            metadata.DebugLinks.Remove(href);
            return metadata;
        }
    }
}