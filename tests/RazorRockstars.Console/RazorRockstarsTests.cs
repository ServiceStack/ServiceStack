using System;
using System.Net;
using System.Threading;
using NUnit.Framework;
using ServiceStack.Text;

namespace RazorRockstars.Console
{
	[TestFixture]
	public class RazorRockstarsTests
	{
        AppHost appHost;
       
        [TestFixtureSetUp]
	    public void TestFixtureSetUp()
	    {
	        appHost = new AppHost();
	        appHost.Init();
            appHost.Start("http://*:1337/");
	    }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Ignore("Debug Run")][Test]
	    public void RunFor10Mins()
	    {
	        Thread.Sleep(TimeSpan.FromMinutes(10));
	    }

		public static string AcceptContentType = "*/*";
		public void Assert200(string url, params string[] containsItems)
		{
            url.Print();
			var text = url.GetStringFromUrl(AcceptContentType, r => {
				if (r.StatusCode != HttpStatusCode.OK)
					Assert.Fail(url + " did not return 200 OK");
			});
			foreach (var item in containsItems)
			{
				if (!text.Contains(item))
				{
					Assert.Fail(item + " was not found in " + url);
				}
			}
		}

		public void Assert200UrlContentType(string url, string contentType)
		{
            url.Print();
            var text = url.GetStringFromUrl(AcceptContentType, r => {
				if (r.StatusCode != HttpStatusCode.OK)
					Assert.Fail(url + " did not return 200 OK: " + r.StatusCode);
				if (!r.ContentType.StartsWith(contentType))
					Assert.Fail(url + " did not return contentType " + contentType);
			});
		}

		public static string Host = "http://localhost:1337";

		static string ViewRockstars = "<!--view:Rockstars.cshtml-->";
		static string ViewRockstars2 = "<!--view:Rockstars2.cshtml-->";
		static string ViewRockstarsMark = "<!--view:RockstarsMark.md-->";
		static string ViewNoModelNoController = "<!--view:NoModelNoController.cshtml-->";
		static string ViewTypedModelNoController = "<!--view:TypedModelNoController.cshtml-->";
        static string ViewPage1 = "<!--view:Page1.cshtml-->";
        static string ViewPage2 = "<!--view:Page2.cshtml-->";
        static string ViewPage3 = "<!--view:Page3.cshtml-->";
        static string ViewPage4 = "<!--view:Page4.cshtml-->";
        static string ViewMarkdownRootPage = "<!--view:MRootPage.md-->";
        static string ViewMPage1 = "<!--view:MPage1.md-->";
        static string ViewMPage2 = "<!--view:MPage2.md-->";
        static string ViewMPage3 = "<!--view:MPage3.md-->";
        static string ViewMPage4 = "<!--view:MPage4.md-->";
        static string ViewRazorPartial = "<!--view:RazorPartial.cshtml-->";
        static string ViewMarkdownPartial = "<!--view:MarkdownPartial.md-->";
        static string ViewRazorPartialModel = "<!--view:RazorPartialModel.cshtml-->";

		static string Template_Layout = "<!--template:_Layout.cshtml-->";
        static string Template_Layout1 = "<!--template:Pages/_Layout.cshtml-->";
        static string Template_Layout2 = "<!--template:Pages/Dir/_Layout.cshtml-->";
        static string TemplateSimpleLayout = "<!--template:SimpleLayout.cshtml-->";
        static string TemplateSimpleLayout2 = "<!--template:SimpleLayout2.cshtml-->";
        static string TemplateHtmlReport = "<!--template:HtmlReport.cshtml-->";
        static string TemplateMarkdownDefault = "<!--template:default.shtml-->";
        static string TemplateMarkdownDefault1 = "<!--template:Pages/default.cshtml-->";
        static string TemplateMarkdownDefault2 = "<!--template:Pages/Dir/default.cshtml-->";
        static string TemplateMarkdownHtmlReport = "<!--template:HtmlReport.shtml-->";

		[Test]
		public void Can_get_page_with_default_view_and_template()
		{
            Assert200(Host + "/rockstars", ViewRockstars, TemplateHtmlReport);
		}

		[Test]
		public void Can_get_page_with_alt_view_and_default_template()
		{
            Assert200(Host + "/rockstars?View=Rockstars2", ViewRockstars2, Template_Layout);
		}
		
		[Test]
		public void Can_get_page_with_alt_viewengine_view_and_default_template()
		{
            Assert200(Host + "/rockstars?View=RockstarsMark", ViewRockstarsMark, TemplateMarkdownHtmlReport);
		}

        [Test]
        public void Can_get_page_with_default_view_and_alt_template()
        {
            Assert200(Host + "/rockstars?Template=SimpleLayout", ViewRockstars, TemplateSimpleLayout);
        }

        [Test]
        public void Can_get_page_with_alt_viewengine_view_and_alt_razor_template()
        {
            Assert200(Host + "/rockstars?View=Rockstars2&Template=SimpleLayout2", ViewRockstars2, TemplateSimpleLayout2);
        }

        [Test]
        public void Can_get_razor_content_pages()
        {
            Assert200(Host + "/TypedModelNoController",
                ViewTypedModelNoController, TemplateSimpleLayout, ViewRazorPartial, ViewMarkdownPartial, ViewRazorPartialModel);
            Assert200(Host + "/nomodelnocontroller",
                ViewNoModelNoController, TemplateSimpleLayout, ViewRazorPartial, ViewMarkdownPartial);
            Assert200(Host + "/pages/page1",
                ViewPage1, Template_Layout1, ViewRazorPartialModel, ViewMarkdownPartial);
            Assert200(Host + "/pages/dir/Page2",
                ViewPage2, Template_Layout2, ViewRazorPartial, ViewMarkdownPartial);
        }

        [Test]
        public void Can_get_markdown_content_pages()
        {
            Assert200(Host + "/MRootPage",
                ViewMarkdownRootPage, TemplateMarkdownDefault);
            Assert200(Host + "/pages/mpage1",
                ViewMPage1, TemplateMarkdownDefault1);
            Assert200(Host + "/pages/dir/mPage2",
                ViewMPage2, TemplateMarkdownDefault2);
        }

    }
}

