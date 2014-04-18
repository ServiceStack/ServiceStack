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
    public class RazorLayoutResolutionTests
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
        public void Root_view_page_can_resolve_sibling_default_layout()
        {
            SetupAllLayoutFiles();

            const string viewPage = "RootViewPage";
            RazorFormat.AddFileAndPage("/Views/RootView.cshtml", viewPage);

            var result = ExecuteViewPage<RootViewResponse>();
            Assert.That(result, Is.EqualTo(RootViewLayout.Replace("@RenderBody()", viewPage)));
        }

        [Test]
        public void Child_view_page_can_resolve_sibling_default_layout()
        {
            SetupAllLayoutFiles();

            const string viewPage = "ChildViewPage";
            RazorFormat.AddFileAndPage("/Views/Child/ChildView.cshtml", viewPage);

            var result = ExecuteViewPage<ChildViewResponse>();
            Assert.That(result, Is.EqualTo(ChildViewLayout.Replace("@RenderBody()", viewPage)));
        }

        [Test]
        public void Child_view_page_without_sibling_default_layout_can_resolve_parent_default_layout()
        {
            SetupAllLayoutFiles();

            const string viewPage = "ChildViewWithoutSiblingLayoutPage";
            RazorFormat.AddFileAndPage("/Views/ChildWithoutLayout/ChildViewWithoutSiblingLayout.cshtml", viewPage);

            var result = ExecuteViewPage<ChildViewWithoutSiblingLayoutResponse>();
            Assert.That(result, Is.EqualTo(RootViewLayout.Replace("@RenderBody()", viewPage)));
        }

        [Test]
        public void Child_view_page_without_sibling_or_parent_default_layout_can_resolve_shared_default_layout()
        {
            SetupSharedLayout();
            SetupChildViewLayout();

            const string viewPage = "ChildViewWithoutSiblingLayoutPage";
            RazorFormat.AddFileAndPage("/Views/ChildWithoutLayout/ChildViewWithoutSiblingLayout.cshtml", viewPage);

            var result = ExecuteViewPage<ChildViewWithoutSiblingLayoutResponse>();
            Assert.That(result, Is.EqualTo(SharedLayout.Replace("@RenderBody()", viewPage)));
        }

        [Test]
        public void Root_content_page_can_resolve_sibling_default_layout()
        {
            SetupAllLayoutFiles();

            const string viewPage = "RootContentPage";
            RazorFormat.AddFileAndPage("/content/root-content.cshtml", viewPage);

            var result = ExecuteContentPage("/content/root-content");
            Assert.That(result, Is.EqualTo(RootContentLayout.Replace("@RenderBody()", viewPage)));
        }

        [Test]
        public void Child_content_page_can_resolve_sibling_default_layout()
        {
            SetupAllLayoutFiles();

            const string viewPage = "ChildContentPage";
            RazorFormat.AddFileAndPage("/content/child/child-content.cshtml", viewPage);

            var result = ExecuteContentPage("/content/child/child-content");
            Assert.That(result, Is.EqualTo(ChildContentLayout.Replace("@RenderBody()", viewPage)));
        }

        [Test]
        public void Child_content_page_without_sibling_default_layout_can_resolve_parent_default_layout()
        {
            SetupAllLayoutFiles();

            const string viewPage = "ChildContentWithoutSiblingLayoutPage";
            RazorFormat.AddFileAndPage("/content/child-without-layout/child-content.cshtml", viewPage);

            var result = ExecuteContentPage("/content/child-without-layout/child-content");
            Assert.That(result, Is.EqualTo(RootContentLayout.Replace("@RenderBody()", viewPage)));
        }

        [Test]
        public void Child_content_page_without_sibling_or_parent_default_layout_can_resolve_shared_default_layout()
        {
            SetupSharedLayout();
            SetupChildContentLayout();

            const string viewPage = "ChildContentWithoutSiblingLayoutPage";
            RazorFormat.AddFileAndPage("/content/child-without-layout/child-content.cshtml", viewPage);

            var result = ExecuteContentPage("/content/child-without-layout/child-content");
            Assert.That(result, Is.EqualTo(SharedLayout.Replace("@RenderBody()", viewPage)));
        }

        [Test]
        public void Default_content_page_can_resolve_sibling_default_layout()
        {
            SetupAllLayoutFiles();

            const string viewPage = "RootDefaultContentPage";
            RazorFormat.AddFileAndPage("/content/" + RazorFormat.DefaultPageName, viewPage);

            var result = ExecuteContentPage("/content");
            Assert.That(result, Is.EqualTo(RootContentLayout.Replace("@RenderBody()", viewPage)));
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
