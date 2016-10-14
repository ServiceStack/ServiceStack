using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.IO;
using System.Xml.Linq;

namespace ServiceStack
{
    // Dummy class to satisfy linked files from SS.Razor project
    public static class HostContext
    {
        public static HostConfig Config { get; } = new HostConfig();

        public static string AppConfigPath
        {
            get
            {
                return appConfigPath ?? GetAppConfigPath();
            }

            set
            {
                appConfigPath = value;
            }
        }

        private static string GetAppConfigPath()
        {
            var configPath = ProjectDir + "web.config";
            if (File.Exists(configPath))
            {
                appConfigPath = configPath;
                return appConfigPath;
            }

            configPath = ProjectDir + "Web.config"; //*nix FS FTW!
            if (File.Exists(configPath))
            {
                appConfigPath = configPath;
                return appConfigPath;
            }

            var appHostDll = new FileInfo(ProjectTargetPath).Name;
            configPath = ProjectDir + $"{appHostDll}.config";
            if (!File.Exists(configPath))
                return null;

            appConfigPath = configPath;
            return appConfigPath;
        }

        public static string ProjectDir { get; set; }
        public static string ProjectTargetPath { get; set; }

        private static string appConfigPath;
    }

    // Dummy class to satisfy linked files from SS.Razor project
    public class HostConfig
    {
        public HashSet<string> RazorNamespaces
        {
            get
            {
                if (razorNamespaces != null)
                    return razorNamespaces;

                razorNamespaces = new HashSet<string>();

                //Infer from <system.web.webPages.razor> - what VS.NET's intell-sense uses
                var configPath = HostContext.AppConfigPath;
                if (configPath != null)
                {
                    var xml = File.ReadAllText(configPath);
                    var doc = XElement.Parse(xml);
                    doc.AnyElement("system.web.webPages.razor")
                        .AnyElement("pages")
                            .AnyElement("namespaces")
                                .AllElements("add").ToList()
                                    .ForEach(x => razorNamespaces.Add(x.AnyAttribute("namespace").Value));
                }

                //E.g. <add key="servicestack.razor.namespaces" value="System,ServiceStack.Text" />
                if (ConfigurationManager.AppSettings[NamespacesAppSettingsKey] != null)
                {
                    var list = new List<string>(ConfigurationManager.AppSettings[NamespacesAppSettingsKey].Split(','));
                    list.ForEach(x => razorNamespaces.Add(x));
                }

                return razorNamespaces;
            }
        }

        private HashSet<string> razorNamespaces;
        private const string NamespacesAppSettingsKey = "servicestack.razor.namespaces";
    }
}
