#if !NETSTANDARD1_6
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Configuration;
using System.Xml.Linq;
using ServiceStack.Configuration;

namespace ServiceStack.Platforms
{
    public partial class PlatformNet : Platform
    {
        const string NamespacesAppSettingsKey = "servicestack.razor.namespaces";

        public override void InitHostConifg(HostConfig config)
        {
            if (config.HandlerFactoryPath == null)
            {
                InferHttpHandlerPath(config);
            }
        }

        public override HashSet<string> GetRazorNamespaces()
        {
            var razorNamespaces = new HashSet<string>();
            //Infer from <system.web.webPages.razor> - what VS.NET's intell-sense uses
            var configPath = GetAppConfigPath();
            if (configPath != null)
            {
                var xml = configPath.ReadAllText();
                var doc = XElement.Parse(xml);
                doc.AnyElement("system.web.webPages.razor")
                    .AnyElement("pages")
                    .AnyElement("namespaces")
                    .AllElements("add").ToList()
                    .ForEach(x => razorNamespaces.Add(x.AnyAttribute("namespace").Value));
            }

            //E.g. <add key="servicestack.razor.namespaces" value="System,ServiceStack.Text" />
            if (ConfigUtils.GetNullableAppSetting(NamespacesAppSettingsKey) != null)
            {
                ConfigUtils.GetListFromAppSetting(NamespacesAppSettingsKey)
                    .ForEach(x => razorNamespaces.Add(x));
            }

            return razorNamespaces;
        }

        public override string GetAppConfigPath()
        {
            if (ServiceStackHost.Instance == null) return null;

            var configPath = "~/web.config".MapHostAbsolutePath();
            if (File.Exists(configPath))
                return configPath;

            configPath = "~/Web.config".MapHostAbsolutePath(); //*nix FS FTW!
            if (File.Exists(configPath))
                return configPath;

            var appHostDll = new FileInfo(ServiceStackHost.Instance.GetType().Assembly.Location).Name;
            configPath = $"~/{appHostDll}.config".MapAbsolutePath();
            return File.Exists(configPath) ? configPath : null;
        }

        private static System.Configuration.Configuration GetAppConfig()
        {
            Assembly entryAssembly;

            //Read the user-defined path in the Web.Config
            if (HostContext.IsAspNetHost)
                return WebConfigurationManager.OpenWebConfiguration("~/");

            if ((entryAssembly = Assembly.GetEntryAssembly()) != null)
                return ConfigurationManager.OpenExeConfiguration(entryAssembly.Location);

            return null;
        }

        private static void InferHttpHandlerPath(HostConfig config)
        {
            try
            {
                var webConfig = GetAppConfig();
                if (webConfig == null) return;

                SetPathsFromConfiguration(config, webConfig, null);

                if (config.MetadataRedirectPath == null)
                {
                    foreach (ConfigurationLocation location in webConfig.Locations)
                    {
                        SetPathsFromConfiguration(config, location.OpenConfiguration(), (location.Path ?? "").ToLower());

                        if (config.MetadataRedirectPath != null) { break; }
                    }
                }

                if (HostContext.IsAspNetHost && config.MetadataRedirectPath == null)
                {
                    throw new ConfigurationErrorsException(
                        "Unable to infer ServiceStack's <httpHandler.Path/> from the Web.Config\n"
                        + "Check with https://github.com/ServiceStack/ServiceStack/wiki/Create-your-first-webservice to ensure you have configured ServiceStack properly.\n"
                        + "Otherwise you can explicitly set your httpHandler.Path by setting: EndpointHostConfig.ServiceStackPath");
                }
            }
            catch (Exception) { }
        }

        private static void SetPathsFromConfiguration(HostConfig config, System.Configuration.Configuration webConfig, string locationPath)
        {
            if (webConfig == null)
                return;

            //standard config
            var handlersSection = webConfig.GetSection("system.web/httpHandlers") as HttpHandlersSection;
            if (handlersSection != null)
            {
                for (var i = 0; i < handlersSection.Handlers.Count; i++)
                {
                    var httpHandler = handlersSection.Handlers[i];
                    if (!httpHandler.Type.StartsWith("ServiceStack"))
                        continue;

                    SetPaths(config, httpHandler.Path, locationPath);
                    break;
                }
            }

            //IIS7+ integrated mode system.webServer/handlers
            var pathsNotSet = config.MetadataRedirectPath == null;
            if (pathsNotSet)
            {
                var webServerSection = webConfig.GetSection("system.webServer");
                var rawXml = webServerSection?.SectionInformation.GetRawXml();
                if (!String.IsNullOrEmpty(rawXml))
                {
                    SetPaths(config, ExtractHandlerPathFromWebServerConfigurationXml(rawXml), locationPath);
                }

                //In some MVC Hosts auto-inferencing doesn't work, in these cases assume the most likely default of "/api" path
                pathsNotSet = config.MetadataRedirectPath == null;
                if (pathsNotSet)
                {
                    var isMvcHost = Type.GetType("System.Web.Mvc.Controller") != null;
                    if (isMvcHost)
                    {
                        SetPaths(config, "api", null);
                    }
                }
            }
        }

        private static string ExtractHandlerPathFromWebServerConfigurationXml(string rawXml)
        {
            return XDocument.Parse(rawXml).Root.Element("handlers")
                .Descendants("add")
                .Where(handler => EnsureHandlerTypeAttribute(handler).StartsWith("ServiceStack"))
                .Select(handler => handler.Attribute("path").Value)
                .FirstOrDefault();
        }

        private static string EnsureHandlerTypeAttribute(XElement handler)
        {
            if (handler.Attribute("type") != null && !String.IsNullOrEmpty(handler.Attribute("type").Value))
            {
                return handler.Attribute("type").Value;
            }
            return String.Empty;
        }

        private static void SetPaths(HostConfig config, string handlerPath, string locationPath)
        {
            if (handlerPath == null) return;

            if (locationPath == null)
            {
                handlerPath = handlerPath.Replace("*", String.Empty);
            }

            config.HandlerFactoryPath = locationPath ??
                                        (String.IsNullOrEmpty(handlerPath) ? null : handlerPath);

            config.MetadataRedirectPath = "metadata";
        }
    }
}
#endif
