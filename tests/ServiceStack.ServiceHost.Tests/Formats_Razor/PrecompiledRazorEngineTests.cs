using System.Linq;
using System.Web;
using NUnit.Framework;
using ServiceStack.IO;

namespace ServiceStack.ServiceHost.Tests.Formats_Razor
{
    [TestFixture]
    public class PrecompiledRazorEngineTests : RazorEngineTests
    {
        public override bool PrecompileEnabled { get { return true; } }

        const string View1Html = "<div class='view1'>@DateTime.Now</div>";
        const string View2Html = "<div class='view2'>@DateTime.Now</div>";
        const string View3Html = "<div class='view3'>@DateTime.Now</div>";

        protected override void InitializeFileSystem(MemoryVirtualFiles fileSystem)
        {
            base.InitializeFileSystem(fileSystem);

            fileSystem.WriteFile("/views/v1.cshtml", View1Html);
            fileSystem.WriteFile("/views/v2.cshtml", View2Html);
            fileSystem.WriteFile("/views/v3.cshtml", View3Html);
        }

        [Test]
        public void Pages_begin_compilation_on_startup()
        {
            foreach (var page in new[] { "v1", "v2", "v3" }.Select(name => RazorFormat.GetViewPage(name)))
            {
                Assert.That(page.MarkedForCompilation || page.IsCompiling || page.IsValid);
            }
        }

        [Test]
        public void New_pages_begin_compilation_when_added()
        {
            const string template = "This is my sample template, Hello @Model.Name!";
            RazorFormat.AddFileAndPage("/simple.cshtml", template);

            var page = RazorFormat.GetContentPage("/simple.cshtml");
            FuncUtils.WaitWhile(() => page.MarkedForCompilation || page.IsCompiling, millisecondTimeout: 5000);
            Assert.That(page.IsValid);
        }

        [Test]
        public void Pages_with_errors_dont_cause_exceptions_on_thread_starting_the_precompilation()
        {
            const string template = "This is a bad template, Hello @SomeInvalidMember.Name!";
            RazorFormat.AddFileAndPage("/simple.cshtml", template);

            var page = RazorFormat.GetContentPage("/simple.cshtml");
            FuncUtils.WaitWhile(() => page.MarkedForCompilation || page.IsCompiling, millisecondTimeout: 5000);
            Assert.That(page.CompileException, Is.Not.Null);
        }

        [Test]
        public void Pages_with_errors_still_throw_exceptions_when_rendering()
        {
            const string template = "This is a bad template, Hello @SomeInvalidMember.Name!";

            Assert.Throws<HttpCompileException>(() => {
                RazorFormat.AddFileAndPage("/simple.cshtml", template);
                RazorFormat.RenderToHtml("/simple.cshtml", new { Name = "World" });
            });
        }
    }

    [TestFixture]
    public class StartupPrecompiledRazorEngineTests : PrecompiledRazorEngineTests
    {
        public override bool PrecompileEnabled { get { return true; } }
        public override bool WaitForPrecompileEnabled { get { return true; } }

        [Test]
        public void Precompilation_finishes_before_returning_from_init()
        {
            foreach (var page in new[] { "v1", "v2", "v3" }.Select(name => RazorFormat.GetViewPage(name)))
            {
                Assert.That(page.IsValid);
            }
        }
    }
}