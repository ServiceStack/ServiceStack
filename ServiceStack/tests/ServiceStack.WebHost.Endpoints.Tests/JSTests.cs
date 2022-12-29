using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Script;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class JSTests
    {
        [Test]
        public void Can_parse_dynamic_json()
        {
            Assert.That(JSON.parse("1"), Is.EqualTo(1));
            Assert.That(JSON.parse("1.1"), Is.EqualTo(1.1));
            Assert.That(JSON.parse("'a'"), Is.EqualTo("a"));
            Assert.That(JSON.parse("\"a\""), Is.EqualTo("a"));
            Assert.That(JSON.parse("{a:1}"), Is.EqualTo(new Dictionary<string, object> { {"a", 1  }}));
            Assert.That(JSON.parse("{\"a\":1}"), Is.EqualTo(new Dictionary<string, object> { {"a", 1  }}));
            Assert.That(JSON.parse("[{a:1},{b:2}]"), Is.EqualTo(new List<object>
            {
                new Dictionary<string, object> { { "a", 1 } },
                new Dictionary<string, object> { { "b", 2 } }
            }));
        }

        public class CustomFilter : ScriptMethods
        {
            public string reverse(string text) => new string(text.Reverse().ToArray());
        }

        [Test]
        public void Can_eval_js()
        {
            var scope = JS.CreateScope(
                args: new Dictionary<string, object>
                {
                    { "arg", "value"}
                }, 
                functions: new CustomFilter());

            Assert.That(JS.eval("arg", scope), Is.EqualTo("value"));

            Assert.That(JS.eval("reverse(arg)", scope), Is.EqualTo("eulav"));

            Assert.That(JS.eval("itemsOf(3, padRight(reverse(arg), 8, '_'))", scope), Is.EqualTo(new List<object> { "eulav___", "eulav___", "eulav___" }));

            Assert.That(JS.eval("{a: itemsOf(3, padRight(reverse(arg), 8, '_')) }", scope), Is.EqualTo(new Dictionary<string, object>
            {
                { "a", new List<object> { "eulav___", "eulav___", "eulav___" } }
            }));
            
            Assert.That(JS.eval("3.itemsOf(arg.reverse().padRight(8, '_'))", scope), Is.EqualTo(new List<object> { "eulav___", "eulav___", "eulav___" }));
        }

    }
}