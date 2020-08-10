using System;
using NUnit.Framework;
using ServiceStack.IO;
using ServiceStack.Script;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class ScriptSandboxIssues
    {
        [Test]
        public void Does_not_allow_multiple_page_arguments()
        {
            var context = new ScriptContext();
            context.VirtualFiles.WriteFile("_layout.html", "The {{page}} layout");
            context.VirtualFiles.WriteFile("page.html", "A {{page}} variable");
            context.Init();

            var page = context.GetPage("page");

            try
            {
                new PageResult(page).RenderScript();
                Assert.Fail("should throw");
            }
            catch (ScriptException e)
            {
                Assert.That(e.InnerException is NotSupportedException);
            }
        }

        [Test]
        public void Does_not_allow_recursive_partials()
        {
            var context = new ScriptContext();
            context.VirtualFiles.WriteFile("partial.html", "A recursive {{'partial' |> partial}}");
            context.Init();

            var page = context.GetPage("partial");

            try
            {
                new PageResult(page).RenderScript();
                Assert.Fail("should throw");
            }
            catch (ScriptException e)
            {
                Assert.That(e.InnerException is NotSupportedException);
            }
        }
    }
}