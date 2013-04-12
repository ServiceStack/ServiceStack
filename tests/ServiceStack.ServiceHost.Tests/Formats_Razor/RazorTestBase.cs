using System.Collections.Generic;
using ServiceStack.Razor2;
using ServiceStack.VirtualPath;

namespace ServiceStack.ServiceHost.Tests.Formats_Razor
{
    public static class MarkdownFormatExtensions
    {
        public static void AddFileAndView(this RazorFormat razorFormat, ViewPageRef viewPage)
        {
            var pathProvider = (InMemoryVirtualPathProvider)razorFormat.VirtualPathProvider;
            pathProvider.AddFile(viewPage.FilePath, viewPage.Contents);
            razorFormat.AddPage(viewPage);
        }

        public static void AddFileAndTemplate(this RazorFormat razorFormat, string filePath, string contents)
        {
            var pathProvider = (InMemoryVirtualPathProvider)razorFormat.VirtualPathProvider;
            pathProvider.AddFile(filePath, contents);
            razorFormat.AddTemplate(filePath, contents);
        }
    }
    
    public class RazorTestBase
	{
		public const string TemplateName = "Template";
		protected const string PageName = "Page";
        protected RazorFormat RazorFormat;

		public RazorFormat AddPage(string websiteTemplate, string pageTemplate)
		{
            RazorFormat.AddTemplate("websiteTemplate", websiteTemplate);
			RazorFormat.AddPage(
				new ViewPageRef(RazorFormat, "/path/to/tpl", PageName, pageTemplate) {
					Template = "websiteTemplate",
				});

			return RazorFormat;
		}

		public RazorFormat AddPage(string pageTemplate)
		{
			RazorFormat.AddPage(
				new ViewPageRef(RazorFormat, "/path/to/tpl", PageName, pageTemplate) {
                    Service = RazorFormat.TemplateService
                });

            RazorFormat.TemplateService.RegisterPage("/path/to/tpl", PageName);
            RazorFormat.TemplateProvider.CompileQueuedPages();

			return RazorFormat;
		}

        protected ViewPageRef AddViewPage(string pageName, string pagePath, string pageContents, string templatePath = null)
        {
            var dynamicPage = new ViewPageRef(RazorFormat,
                pagePath, pageName, pageContents, RazorPageType.ViewPage) {
                    Template = templatePath,
                    Service = RazorFormat.TemplateService
                };

            RazorFormat.AddPage(dynamicPage);

            RazorFormat.TemplateService.RegisterPage(pagePath, pageName);
            RazorFormat.TemplateProvider.CompileQueuedPages();
            
            return dynamicPage;
        }

		public string RenderToHtml(string pageTemplate, Dictionary<string, object> scopeArgs)
		{
			AddPage(pageTemplate);
			var template = RazorFormat.ExecuteTemplate(scopeArgs, PageName, null);
			return template.Result;
		}

		public string RenderToHtml(string pageTemplate, Dictionary<string, object> scopeArgs, string websiteTemplate)
		{
			AddPage(pageTemplate);
			var template = RazorFormat.ExecuteTemplate(scopeArgs, PageName, websiteTemplate);
			return template.Result;
		}

		public string RenderToHtml<T>(string pageTemplate, T model)
		{
			AddPage(pageTemplate);
			var template = RazorFormat.ExecuteTemplate(model, PageName, null);
			return template.Result;
		}
	}

}
