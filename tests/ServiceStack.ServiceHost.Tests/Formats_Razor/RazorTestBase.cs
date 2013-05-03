using System;
using System.Collections.Generic;
using ServiceStack.Razor;
using ServiceStack.Razor.Managers;
using ServiceStack.VirtualPath;

namespace ServiceStack.ServiceHost.Tests.Formats_Razor
{
    public static class RazorFormatExtensions
    {
        public static void AddFileAndTemplate(this RazorFormat razorFormat, string filePath, string contents)
        {
            var pathProvider = (IWriteableVirtualPathProvider)razorFormat.VirtualPathProvider;
            pathProvider.AddFile(filePath, contents);
            razorFormat.AddPage(filePath);
        }
    }
    
    public class RazorTestBase
	{
		public const string TemplateName = "Template";
		protected const string PageName = "Page";
        protected RazorFormat RazorFormat;

		public RazorFormat AddPage(string websiteTemplate, string pageTemplate)
		{
            throw new NotImplementedException();
            //RazorFormat.AddTemplate("websiteTemplate", websiteTemplate);
            //RazorFormat.AddPage(
            //    new ViewPageRef(RazorFormat, "/path/to/tpl", PageName, pageTemplate) {
            //        Template = "websiteTemplate",
            //    });

            //return RazorFormat;
		}

		public RazorFormat AddPage(string pageTemplate)
		{
            throw new NotImplementedException();
            //RazorFormat.AddPage(
            //    new ViewPageRef(RazorFormat, "/path/to/tpl", PageName, pageTemplate) {
            //        Service = RazorFormat.TemplateService
            //    });

            //RazorFormat.TemplateService.RegisterPage("/path/to/tpl", PageName);
            //RazorFormat.TemplateProvider.CompileQueuedPages();

            //return RazorFormat;
		}

        protected RazorPage AddViewPage(string pageName, string pagePath, string pageContents, string templatePath = null)
        {
            throw new NotImplementedException();
            //var dynamicPage = new ViewPageRef(RazorFormat,
            //    pagePath, pageName, pageContents, RazorPageType.ViewPage) {
            //        Template = templatePath,
            //        Service = RazorFormat.TemplateService
            //    };

            //RazorFormat.AddPage(dynamicPage);

            //RazorFormat.TemplateService.RegisterPage(pagePath, pageName);
            //RazorFormat.TemplateProvider.CompileQueuedPages();
            
            //return dynamicPage;
        }

		public string RenderToHtml(string pageTemplate, Dictionary<string, object> scopeArgs)
		{
            throw new NotImplementedException();
            //AddPage(pageTemplate);
            //var template = RazorFormat.ExecuteTemplate(scopeArgs, PageName, null);
            //return template.Result;
		}

		public string RenderToHtml(string pageTemplate, Dictionary<string, object> scopeArgs, string websiteTemplate)
		{
            throw new NotImplementedException();
            //AddPage(pageTemplate);
            //var template = RazorFormat.ExecuteTemplate(scopeArgs, PageName, websiteTemplate);
            //return template.Result;
		}

		public string RenderToHtml<T>(string pageTemplate, T model)
		{
            throw new NotImplementedException();
            //AddPage(pageTemplate);
            //var template = RazorFormat.ExecuteTemplate(model, PageName, null);
            //return template.Result;
		}
	}

}
