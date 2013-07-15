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

        static HtmlTemplates()
        {
            IndexOperationsTemplate = LoadHtmlTemplate("IndexOperations.html");
            OperationControlTemplate = LoadHtmlTemplate("OperationControl.html");
            OperationsControlTemplate = LoadHtmlTemplate("OperationsControl.html");
        }

        private static string LoadHtmlTemplate(string templateName)
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
