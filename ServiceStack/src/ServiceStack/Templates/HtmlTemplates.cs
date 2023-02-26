using System.IO;

namespace ServiceStack.Templates
{
    /// <summary>
    /// Load Embedded Resource Templates in ServiceStack.
    /// To get ServiceStack to use your own instead just add a copy of one or more of the following to your Web Root:
    /// ~/Templates/IndexOperations.html
    /// ~/Templates/OperationControl.html
    /// ~/Templates/HtmlFormat.html
    /// </summary>
    public static class HtmlTemplates
    {
        public static string HtmlFormatName { get; set; } = "HtmlFormat.html";
        
        public static string GetIndexOperationsTemplate()
        {
            return LoadTemplate("IndexOperations.html")
                .Replace("{{serviceStackLogoDataUriLight}}", Svg.Fill(Svg.GetDataUri(Svg.Logos.ServiceStack), Svg.LightColor));
        }

        public static string GetOperationControlTemplate()
        {
            return LoadTemplate("OperationControl.html")
                .Replace("{{serviceStackLogoDataUriLight}}", Svg.Fill(Svg.GetDataUri(Svg.Logos.ServiceStack), Svg.LightColor));
        }

        public static string GetMetadataDebugTemplate()
        {
            return LoadTemplate("MetadataDebug.html")
                .Replace("{{serviceStackLogoDataUriLight}}", Svg.Fill(Svg.GetDataUri(Svg.Logos.ServiceStack), Svg.LightColor));
        }

        public static string GetLoginTemplate()
        {
            var appHost = HostContext.AppHost;
            return LoadTemplate("login.html")
                .Replace("${BaseUrl}", appHost.ResolveStaticBaseUrl())
                .Replace("${ServiceName}", appHost.ServiceName);
        }

        public static string GetHtmlFormatTemplate() => LoadTemplate(HtmlFormatName);

        public static string GetSvgTemplatePath() => GetTemplatePath("svg.html");

        private static string LoadTemplate(string templateName)
        {
            var templatePath = GetTemplatePath(templateName);
            var file = HostContext.VirtualFileSources.GetFile(templatePath);
            if (file == null)
                throw new FileNotFoundException("Could not load HTML template: " + templatePath, templateName);

            var contents = file.ReadAllText();
            return contents;
        }

        public static string GetTemplatePath(string templateName) => "/Templates/" + templateName;

        public static string GetHtmlRedirectTemplate(string url)
        {
            var html = LoadTemplate("Redirect.html");
            return html.Replace("{Url}", url);
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
