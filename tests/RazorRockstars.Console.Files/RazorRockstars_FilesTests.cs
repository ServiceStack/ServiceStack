using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.Razor2;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceInterface.Testing;
using ServiceStack.Text;

namespace RazorRockstars.Console.Files
{
    [TestFixture]
    public class RazorRockstars_FilesTests
    {
        private const string ListeningOn = "http://*:1337/";
        public const string Host = "http://localhost:1337";

        //private const string ListeningOn = "http://*:1337/subdir/subdir2/";
        //private const string Host = "http://localhost:1337/subdir/subdir2";

        private const string BaseUri = Host + "/";

        AppHost appHost;

        Stopwatch startedAt;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            LogManager.LogFactory = new ConsoleLogFactory();
            startedAt = Stopwatch.StartNew();
            appHost = new AppHost();
            appHost.Init();
            appHost.Start(ListeningOn);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            "Time Taken {0}ms".Fmt(startedAt.ElapsedMilliseconds).Print();
            appHost.Dispose();
        }

        [Ignore("Debug Run")]
        [Test]
        public void RunFor10Mins()
        {
            Thread.Sleep(TimeSpan.FromMinutes(10));
        }
        
        [Test]
        public void Does_not_use_same_razor_page_instance()
        {
            var html = GetRazorInstanceHtml();
            Assert.That(html, Is.StringContaining("<h5>Counter: 1</h5>"));
            
            html = GetRazorInstanceHtml();
            Assert.That(html, Is.StringContaining("<h5>Counter: 1</h5>"));
        }

        private static string GetRazorInstanceHtml()
        {
            var razorFormat = RazorFormat.Instance;
            var mockReq = new MockHttpRequest {OperationName = "RazorInstance"};
            var mockRes = new MockHttpResponse();
            var dto = new RockstarsResponse {Results = Rockstar.SeedData.ToList()};
            razorFormat.ProcessRequest(mockReq, mockRes, dto);
            var html = mockRes.ReadAsString();
            return html;
        }
        
        public static string AcceptContentType = "*/*";
        public void Assert200(string url, params string[] containsItems)
        {
            url.Print();
            var text = url.GetStringFromUrl(AcceptContentType, responseFilter:r => {
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
            url.GetStringFromUrl(AcceptContentType, responseFilter:r => {
                if (r.StatusCode != HttpStatusCode.OK)
                    Assert.Fail(url + " did not return 200 OK: " + r.StatusCode);
                if (!r.ContentType.StartsWith(contentType))
                    Assert.Fail(url + " did not return contentType " + contentType);
            });
        }

        static string ViewRockstars = "<!--view:Rockstars.cshtml-->";
        static string ViewRockstars2 = "<!--view:Rockstars2.cshtml-->";
        static string ViewRockstars3 = "<!--view:Rockstars3.cshtml-->";
        static string ViewRockstarsMark = "<!--view:RockstarsMark.md-->";
        static string ViewNoModelNoController = "<!--view:NoModelNoController.cshtml-->";
        static string ViewTypedModelNoController = "<!--view:TypedModelNoController.cshtml-->";
        static string ViewCachedAllReqstars = "<!--view:CachedAllReqstars.cshtml-->";
        static string ViewIList = "<!--view:IList.cshtml-->";
        static string ViewList = "<!--view:List.cshtml-->";
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
        static string ViewPartialChildModel = "<!--view:PartialChildModel.cshtml-->";

        static string View_Default = "<!--view:default.cshtml-->";
        static string View_Pages_Default = "<!--view:Pages/default.cshtml-->";
        static string View_Pages_Dir_Default = "<!--view:Pages/Dir/default.cshtml-->";
        static string ViewM_Pages_Dir2_Default = "<!--view:Pages/Dir2/default.md-->";

        static string Template_Layout = "<!--template:_Layout.cshtml-->";
        static string Template_Pages_Layout = "<!--template:Pages/_Layout.cshtml-->";
        static string Template_Pages_Dir_Layout = "<!--template:Pages/Dir/_Layout.cshtml-->";
        static string Template_SimpleLayout = "<!--template:SimpleLayout.cshtml-->";
        static string Template_SimpleLayout2 = "<!--template:SimpleLayout2.cshtml-->";
        static string Template_HtmlReport = "<!--template:HtmlReport.cshtml-->";
        static string Template_PartialModel = "<!--template:PartialModel.cshtml-->";

        static string TemplateM_Layout = "<!--template:_Layout.shtml-->";
        static string TemplateM_Pages_Layout = "<!--template:Pages/_Layout.shtml-->";
        static string TemplateM_Pages_Dir_Layout = "<!--template:Pages/Dir/_Layout.shtml-->";
        static string TemplateM_HtmlReport = "<!--template:HtmlReport.shtml-->";

        [Test]
        public void Can_get_metadata_page()
        {
            Assert200(BaseUri.CombineWith("metadata"));
        }

        [Test]
        public void Can_get_Rockstars_service()
        {
            var client = new JsonServiceClient(BaseUri);
            var response = client.Get<RockstarsResponse>("rockstars");
            Assert.That(response.Results.Count, Is.EqualTo(Rockstar.SeedData.Length));
        }

        [Test]
        public void Can_get_page_with_default_view_and_template()
        {
            Assert200(Host + "/rockstars", ViewRockstars, Template_HtmlReport);
        }

        [Test]
        public void Can_get_page_with_alt_view_and_default_template()
        {
            Assert200(Host + "/rockstars?View=Rockstars2", ViewRockstars2, Template_Layout);
        }

        [Test]
        public void Can_get_page_with_alt_viewengine_view_and_default_template()
        {
            Assert200(Host + "/rockstars?View=RockstarsMark", ViewRockstarsMark, TemplateM_HtmlReport);
        }

        [Test]
        public void Can_get_page_with_default_view_and_alt_template()
        {
            Assert200(Host + "/rockstars?Template=SimpleLayout", ViewRockstars, Template_SimpleLayout);
        }

        [Test]
        public void Can_get_page_with_alt_viewengine_view_and_alt_razor_template()
        {
            Assert200(Host + "/rockstars?View=Rockstars2&Template=SimpleLayout2", ViewRockstars2, Template_SimpleLayout2);
        }

        [Test]
        public void Can_get_razor_content_pages()
        {
            Assert200(Host + "/TypedModelNoController",
                ViewTypedModelNoController, Template_SimpleLayout, ViewRazorPartial, ViewMarkdownPartial, ViewRazorPartialModel);
            Assert200(Host + "/nomodelnocontroller",
                ViewNoModelNoController, Template_SimpleLayout, ViewRazorPartial, ViewMarkdownPartial);
            Assert200(Host + "/pages/page1",
                ViewPage1, Template_Pages_Layout, ViewRazorPartialModel, ViewMarkdownPartial);
            Assert200(Host + "/pages/dir/Page2",
                ViewPage2, Template_Pages_Dir_Layout, ViewRazorPartial, ViewMarkdownPartial);
            Assert200(Host + "/pages/dir2/Page3",
                ViewPage3, Template_Pages_Layout, ViewRazorPartial, ViewMarkdownPartial);
            Assert200(Host + "/pages/dir2/Page4",
                ViewPage4, Template_HtmlReport, ViewRazorPartial, ViewMarkdownPartial);
        }

        [Test]
        public void Can_get_razor_content_pages_with_partials()
        {
            Assert200(Host + "/pages/dir2/Page4",
                ViewPage4, Template_HtmlReport, ViewRazorPartial, ViewMarkdownPartial, ViewMPage3);
        }

        [Test]
        public void Can_get_markdown_content_pages()
        {
            Assert200(Host + "/MRootPage",
                ViewMarkdownRootPage, TemplateM_Layout);
            Assert200(Host + "/pages/mpage1",
                ViewMPage1, TemplateM_Pages_Layout);
            Assert200(Host + "/pages/dir/mPage2",
                ViewMPage2, TemplateM_Pages_Dir_Layout);
        }

        [Test]
        public void Redirects_when_trying_to_get_razor_page_with_extension()
        {
            Assert200(Host + "/pages/dir2/Page4.cshtml",
                ViewPage4, Template_HtmlReport, ViewRazorPartial, ViewMarkdownPartial, ViewMPage3);
        }

        [Test]
        public void Redirects_when_trying_to_get_markdown_page_with_extension()
        {
            Assert200(Host + "/pages/mpage1.md",
                ViewMPage1, TemplateM_Pages_Layout);
        }

        [Test]
        public void Can_get_default_razor_pages()
        {
            Assert200(Host + "/",
                View_Default, Template_SimpleLayout, ViewRazorPartial, ViewMarkdownPartial, ViewRazorPartialModel);
            Assert200(Host + "/Pages/",
                View_Pages_Default, Template_Pages_Layout, ViewRazorPartial, ViewMarkdownPartial, ViewRazorPartialModel);
            Assert200(Host + "/Pages/Dir/",
                View_Pages_Dir_Default, Template_SimpleLayout, ViewRazorPartial, ViewMarkdownPartial, ViewRazorPartialModel);
        }

        [Test]
        public void Can_get_default_markdown_pages()
        {
            Assert200(Host + "/Pages/Dir2/",
                ViewM_Pages_Dir2_Default, TemplateM_Pages_Layout);
        }

        [Test] //Good for testing adhoc compilation
        public void Can_get_last_view_template_compiled()
        {
            Assert200(Host + "/rockstars?View=Rockstars3", ViewRockstars3, Template_SimpleLayout2);
        }

        [Test]
        public void Can_get_razor_view_with_interface_response()
        {
            Assert200(Host + "/ilist1/IList", ViewIList, Template_HtmlReport);
            Assert200(Host + "/ilist2/IList", ViewIList, Template_HtmlReport);
            Assert200(Host + "/ilist3/IList", ViewIList, Template_HtmlReport);

            Assert200(Host + "/ilist1/List", ViewList, Template_HtmlReport);
            Assert200(Host + "/ilist2/List", ViewList, Template_HtmlReport);
            Assert200(Host + "/ilist3/List", ViewList, Template_HtmlReport);
        }

        [Test]
        public void Can_get_PartialModel()
        {
            var containsItems = new List<string>
                {
                    Template_PartialModel,
                    ViewPartialChildModel,
                };

            5.Times(x => containsItems.Add("<input id=\"SomeProperty\" name=\"SomeProperty\" type=\"text\" value=\"value " + x + "\" />"));

            Assert200(Host + "/partialmodel", containsItems.ToArray());
        }

        [Test]
        public void Can_get_RequestPathInfo_in_PartialChildModel()
        {
            Assert200(Host + "/partialmodel", Template_PartialModel, ViewPartialChildModel, "PathInfo: <b>/partialmodel</b>");
        }

    }
}

