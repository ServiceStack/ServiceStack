using System;
using System.IO;
using ServiceStack.Utils;

namespace ServiceStack.WebHost.Endpoints.Support.Templates
{
    public static class HtmlTemplates
    {
        private static string indexOperationsTemplate;
        public static string IndexOperationsTemplate
        {
            get
            {
                return indexOperationsTemplate ??
                       (indexOperationsTemplate = LoadEmbeddedHtmlTemplate("IndexOperations.html"));
            }
            set { indexOperationsTemplate = value; }
        }

        private static string operationControlTemplate;
        public static string OperationControlTemplate
        {
            get
            {
                return operationControlTemplate ??
                       (operationControlTemplate = LoadEmbeddedHtmlTemplate("OperationControl.html"));
            }
            set { operationControlTemplate = value; }
        }

        private static string operationsControlTemplate;
        public static string OperationsControlTemplate
        {
            get
            {
                return operationsControlTemplate ??
                       (operationsControlTemplate = LoadEmbeddedHtmlTemplate("OperationsControl.html"));
            }
            set { operationsControlTemplate = value; }
        }


        static HtmlTemplates()
        {
            if (EndpointHost.Config.UseCustomMetadataTemplates)
            {
                TryLoadExternal("IndexOperations.html", ref indexOperationsTemplate);
                TryLoadExternal("OperationControl.html", ref operationControlTemplate);
                TryLoadExternal("OperationsControl.html", ref operationsControlTemplate);
            }
        }

        private static bool TryLoadExternal(string templateName, ref string template)
        {
            try
            {
                var staticFilePath = PathUtils.CombinePaths(
                    EndpointHost.AppHost.VirtualPathProvider.RootDirectory.RealPath, 
                    EndpointHost.Config.MetadataCustomPath, 
                    templateName);

                template = File.ReadAllText(staticFilePath);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private static string LoadEmbeddedHtmlTemplate(string templateName)
        {
            var resourceNamespace = typeof(HtmlTemplates).Namespace + ".Html.";
            var stream = typeof(HtmlTemplates).Assembly.GetManifestResourceStream(resourceNamespace + templateName);
            if (stream == null)
            {
                throw new FileNotFoundException(
                    "Could not load HTML template embedded resource " + templateName,
                    templateName);
            }
            using (var streamReader = new StreamReader(stream))
            {
                return streamReader.ReadToEnd();
            }
        }

        public static string Format(string template, params object[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                template = template.Replace(@"{" + i + "}", (args[i] ?? "").ToString());
            }
            return template;
        }

    }
}
