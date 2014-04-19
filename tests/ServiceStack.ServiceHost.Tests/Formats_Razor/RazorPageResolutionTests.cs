using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Razor;
using ServiceStack.Testing;
using ServiceStack.VirtualPath;

namespace ServiceStack.ServiceHost.Tests.Formats_Razor
{
    [TestFixture]
    public class RazorPageResolutionTests
    {
        const string SharedLayout = "SharedLayout: @RenderBody()";
        const string RootViewLayout = "RootViewLayout: @RenderBody()";
        const string ChildViewLayout = "ChildViewLayout: @RenderBody()";
        const string RootContentLayout = "RootContentLayout: @RenderBody()";
        const string ChildContentLayout = "ChildContentLayout: @RenderBody()";

        private ServiceStackHost appHost;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new BasicAppHost().Init();
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        protected RazorFormat RazorFormat;

        [SetUp]
        public void OnBeforeEachTest()
        {
            RazorFormat.Instance = null;

            var fileSystem = new InMemoryVirtualPathProvider(new BasicAppHost());

            RazorFormat = new RazorFormat
            {
                VirtualPathProvider = fileSystem,
                EnableLiveReload = false,
            }.Init();
        }

        private void SetupSharedLayout() { RazorFormat.AddFileAndPage("/Views/Shared/_Layout.cshtml", SharedLayout); }
        private void SetupRootViewLayout() { RazorFormat.AddFileAndPage("/Views/_Layout.cshtml", RootViewLayout); }
        private void SetupChildViewLayout() { RazorFormat.AddFileAndPage("/Views/Child/_Layout.cshtml", ChildViewLayout); }
        private void SetupRootContentLayout() { RazorFormat.AddFileAndPage("/content/_Layout.cshtml", RootContentLayout); }
        private void SetupChildContentLayout() { RazorFormat.AddFileAndPage("/content/child/_Layout.cshtml", ChildContentLayout ); }

        private void SetupAllLayoutFiles()
        {
            SetupSharedLayout();
            SetupRootViewLayout();
            SetupChildViewLayout();
            SetupRootContentLayout();
            SetupChildContentLayout();
        }
        
        [Test]
        public void Can_resolve_content_page_by_filename()
        {
            const string content = "ContentPage";
            RazorFormat.AddFileAndPage("/content/page.cshtml", content);

            var resultWithExtension = ExecuteContentPage("/content/page.cshtml");
            Assert.That(resultWithExtension, Is.EqualTo(content));

            var resultWithoutExtension = ExecuteContentPage("/content/page");
            Assert.That(resultWithoutExtension, Is.EqualTo(content));
        }

        [Test]
        public void Content_page_resolution_is_not_case_sensitive()
        {
            const string content = "ContentPage";
            RazorFormat.AddFileAndPage("/Content/Page.cshtml", content);

            var result = ExecuteContentPage("/content/page");
            Assert.That(result, Is.EqualTo(content));
        }

        [Test]
        public void Default_content_page_resolution_works_at_root()
        {
            const string content = "DefaultContentPage";
            RazorFormat.AddFileAndPage("/default.cshtml", content);

            var result = ExecuteContentPage("/");
            Assert.That(result, Is.EqualTo(content));
        }

        [Test]
        public void Default_content_page_resolution_works_in_folder()
        {
            const string content = "DefaultContentPage";
            RazorFormat.AddFileAndPage("/content/default.cshtml", content);

            var result = ExecuteContentPage("/content");
            Assert.That(result, Is.EqualTo(content));
        }

        [Test]
        public void Root_view_page_can_resolve_sibling_default_layout()
        {
            SetupAllLayoutFiles();

            const string viewBody = "RootViewPage";
            RazorFormat.AddFileAndPage("/Views/RootView.cshtml", viewBody);

            var result = ExecuteViewPage<RootViewResponse>();
            Assert.That(result, Is.EqualTo(RootViewLayout.Replace("@RenderBody()", viewBody)));
        }

        [Test]
        public void Child_view_page_can_resolve_sibling_default_layout()
        {
            SetupAllLayoutFiles();

            const string viewBody = "ChildViewPage";
            RazorFormat.AddFileAndPage("/Views/Child/ChildView.cshtml", viewBody);

            var result = ExecuteViewPage<ChildViewResponse>();
            Assert.That(result, Is.EqualTo(ChildViewLayout.Replace("@RenderBody()", viewBody)));
        }

        [Test]
        public void Child_view_page_without_sibling_default_layout_can_resolve_parent_default_layout()
        {
            SetupAllLayoutFiles();

            const string viewBody = "ChildViewWithoutSiblingLayoutPage";
            RazorFormat.AddFileAndPage("/Views/ChildWithoutLayout/ChildViewWithoutSiblingLayout.cshtml", viewBody);

            var result = ExecuteViewPage<ChildViewWithoutSiblingLayoutResponse>();
            Assert.That(result, Is.EqualTo(RootViewLayout.Replace("@RenderBody()", viewBody)));
        }

        [Test]
        public void Child_view_page_without_sibling_or_parent_default_layout_can_resolve_shared_default_layout()
        {
            SetupSharedLayout();
            SetupChildViewLayout();

            const string viewBody = "ChildViewWithoutSiblingLayoutPage";
            RazorFormat.AddFileAndPage("/Views/ChildWithoutLayout/ChildViewWithoutSiblingLayout.cshtml", viewBody);

            var result = ExecuteViewPage<ChildViewWithoutSiblingLayoutResponse>();
            Assert.That(result, Is.EqualTo(SharedLayout.Replace("@RenderBody()", viewBody)));
        }

        [Test]
        public void Root_content_page_can_resolve_sibling_default_layout()
        {
            SetupAllLayoutFiles();

            const string contentBody = "RootContentPage";
            RazorFormat.AddFileAndPage("/content/root-content.cshtml", contentBody);

            var result = ExecuteContentPage("/content/root-content.cshtml");
            Assert.That(result, Is.EqualTo(RootContentLayout.Replace("@RenderBody()", contentBody)));
        }

        [Test]
        public void Child_content_page_can_resolve_sibling_default_layout()
        {
            SetupAllLayoutFiles();

            const string contentBody = "ChildContentPage";
            RazorFormat.AddFileAndPage("/content/child/child-content.cshtml", contentBody);

            var result = ExecuteContentPage("/content/child/child-content");
            Assert.That(result, Is.EqualTo(ChildContentLayout.Replace("@RenderBody()", contentBody)));
        }

        [Test]
        public void Child_content_page_without_sibling_default_layout_can_resolve_parent_default_layout()
        {
            SetupAllLayoutFiles();

            const string contentBody = "ChildContentWithoutSiblingLayoutPage";
            RazorFormat.AddFileAndPage("/content/child-without-layout/child-content.cshtml", contentBody);

            var result = ExecuteContentPage("/content/child-without-layout/child-content");
            Assert.That(result, Is.EqualTo(RootContentLayout.Replace("@RenderBody()", contentBody)));
        }

        [Test]
        public void Child_content_page_without_sibling_or_parent_default_layout_can_resolve_shared_default_layout()
        {
            SetupSharedLayout();
            SetupChildContentLayout();

            const string contentBody = "ChildContentWithoutSiblingLayoutPage";
            RazorFormat.AddFileAndPage("/content/child-without-layout/child-content.cshtml", contentBody);

            var result = ExecuteContentPage("/content/child-without-layout/child-content");
            Assert.That(result, Is.EqualTo(SharedLayout.Replace("@RenderBody()", contentBody)));
        }

        [Test]
        public void Default_content_page_can_resolve_sibling_default_layout()
        {
            SetupAllLayoutFiles();

            const string contentBody = "RootDefaultContentPage";
            RazorFormat.AddFileAndPage("/content/" + RazorFormat.DefaultPageName, contentBody);

            var result = ExecuteContentPage("/content");
            Assert.That(result, Is.EqualTo(RootContentLayout.Replace("@RenderBody()", contentBody)));
        }

        [Test]
        public void Content_page_does_not_resolve_root_view_folder_default_layout()
        {
            SetupRootViewLayout();

            const string contentBody = "ArbitraryContentPage";
            RazorFormat.AddFileAndPage("/content/arbitrary-content.cshtml", contentBody);

            var result = ExecuteContentPage("/content/arbitrary-content");
            Assert.That(result, Is.EqualTo(contentBody));
        }

        private string ExecuteViewPage<T>() where T : new()
        {
            var mockReq = new MockHttpRequest { OperationName = typeof(T).Name.Replace("Response","") };
            var mockRes = new MockHttpResponse();
            var dto = new T();
            RazorFormat.ProcessRequest(mockReq, mockRes, dto);
            return mockRes.ReadAsString();
        }

        private string ExecuteContentPage(string path)
        {
            var mockReq = new MockHttpRequest
            {
                OperationName = "Razor_PageResolver",
                PathInfo = path,
            };
            var mockRes = new MockHttpResponse();
            RazorFormat.ProcessContentPageRequest(mockReq, mockRes);
            return mockRes.ReadAsString();
        }

        public class RootView {}
        public class RootViewResponse { }
        public class ChildView { }
        public class ChildViewResponse { }
        public class ChildViewWithoutSiblingLayout { }
        public class ChildViewWithoutSiblingLayoutResponse { }
    }
}
