using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ServiceStack.Razor2;
using ServiceStack.ServiceInterface.Testing;
using ServiceStack.Text;
using ServiceStack.VirtualPath;

namespace ServiceStack.ServiceHost.Tests.Formats_Razor
{
    [TestFixture]
    public class StandAloneExampleTests
    {
        [Test]
        public void Simple_static_example()
        {
            var razor = new RazorFormat
            {
                //DefaultBaseType = typeof(CustomRazorBasePage<>), //Change custom base ViewPage
                VirtualPathProvider = new InMemoryVirtualPathProvider(new BasicAppHost()),
                TemplateProvider = { CompileInParallelWithNoOfThreads = 0 },                
            };
            razor.Init();

            razor.AddPage("Hello @Model.Name! Welcome to Razor!");
            var html = razor.RenderToHtml(new { Name = "World" });
            html.Print();

            Assert.That(html, Is.EqualTo("Hello World! Welcome to Razor!"));
        }
    }

    public static class RazorFormatExtensions
    {
        public static RazorFormat AddPage(this RazorFormat razor, string pageTemplate, string pageName = "Page")
        {
            razor.AddPage(
                new ViewPageRef(razor, "/path/to/tpl", pageName, pageTemplate)
                {
                    Service = razor.TemplateService
                });

            razor.TemplateService.RegisterPage("/path/to/tpl", pageName);
            razor.TemplateProvider.CompileQueuedPages();

            return razor;
        }

        public static string RenderToHtml<T>(this RazorFormat razor, T model, string pageName = "Page")
        {
            var template = razor.ExecuteTemplate(model, pageName, null);
            return template.Result;
        }
    }
}
