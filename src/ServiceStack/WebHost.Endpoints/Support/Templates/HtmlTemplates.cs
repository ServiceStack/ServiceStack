using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;

namespace ServiceStack.WebHost.Endpoints.Support.Templates
{
    public static class HtmlTemplates
    {
        public static string IndexOperationsTemplate;
        public static string OperationControlTemplate;
        public static string OperationsControlTemplate;

        private const string IndexOperationsFileName = "IndexOperations.html";
        private const string OperationControlFileName = "OperationControl.html";
        private const string OperationsControlFileName = "OperationsControl.html";


        static HtmlTemplates()
        {
            var UseCustomPath = EndpointHost.Config.UseCustomMetadataTemplates;

            if (UseCustomPath)
            {
                IndexOperationsTemplate = LoadExternal(IndexOperationsFileName);
                OperationControlTemplate = LoadExternal(OperationControlFileName);
                OperationsControlTemplate = LoadExternal(OperationsControlFileName);
            }
            else
            {
                IndexOperationsTemplate = LoadEmbeddedHtmlTemplate(IndexOperationsFileName);
                OperationControlTemplate = LoadEmbeddedHtmlTemplate(OperationControlFileName);
                OperationsControlTemplate = LoadEmbeddedHtmlTemplate(OperationsControlFileName);
            }
        }

        private static string LoadExternal(string templateName)
        {
            try
            {
                return File.ReadAllText(Path.Combine(EndpointHost.AppHost.VirtualPathProvider.RootDirectory.RealPath + "/" + EndpointHost.Config.MetadataCustomPath, templateName));
            }
            catch (Exception ex)
            {
                return LoadEmbeddedHtmlTemplate(templateName);
            }
        }

        private static string LoadEmbeddedHtmlTemplate(string templateName)
        {
            string _resourceNamespace = typeof(HtmlTemplates).Namespace + ".Html.";
            var stream = typeof(HtmlTemplates).Assembly.GetManifestResourceStream(_resourceNamespace + templateName);
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

    }
}
