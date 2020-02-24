using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Script;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class JsAssignmentTests
    {
        [Test]
        public void Can_assign_local_Variables()
        {
            var context = new ScriptContext().Init();
            
            var pageResult = new PageResult(context.OneTimePage("{{ var a = 1 }}a == {{a}}"));
            var output = pageResult.RenderScript();
            Assert.That(output, Is.EqualTo("a == 1"));

            pageResult = new PageResult(context.OneTimePage("{{ var a = 1, b = 1 + 1 }}b == {{b}}"));
            output = pageResult.RenderScript();
            Assert.That(output, Is.EqualTo("b == 2"));
        }
        
        [Test]
        public void Can_assign_global_Variables()
        {
            var context = new ScriptContext().Init();
            
            var pageResult = new PageResult(context.OneTimePage("a == {{a = 1}}"));
            var output = pageResult.RenderScript();
            Assert.That(output, Is.EqualTo("a == 1"));
            Assert.That(pageResult.Args["a"], Is.EqualTo(1));

            pageResult = new PageResult(context.OneTimePage("g == {{global.g = 1}}"));
            output = pageResult.RenderScript();
            Assert.That(pageResult.Args["g"], Is.EqualTo(1));
            Assert.That(output, Is.EqualTo("g == 1"));

            pageResult = new PageResult(context.OneTimePage("g == {{global['g'] = 2}}"));
            output = pageResult.RenderScript();
            Assert.That(pageResult.Args["g"], Is.EqualTo(2));
            Assert.That(output, Is.EqualTo("g == 2"));
        }

        [Test]
        public void Can_assign_collections()
        {
            var context = new ScriptContext {
                Args = {
                    ["list"] = new List<int> { 1, 2, 3 },
                    ["array"] = new[] { 1, 2, 3 },
                }
            }.Init();

            var pageResult = new PageResult(context.OneTimePage("list[1] == {{ list[1] = 4 }}"));
            var output = pageResult.RenderScript();
            Assert.That(output, Is.EqualTo("list[1] == 4"));
            Assert.That(((List<int>)context.Args["list"])[1], Is.EqualTo(4));

            pageResult = new PageResult(context.OneTimePage("array[1] == {{ array[1] = 4 }}"));
            output = pageResult.RenderScript();
            Assert.That(output, Is.EqualTo("array[1] == 4"));
            Assert.That(((int[])context.Args["array"])[1], Is.EqualTo(4));
        }
    }
}