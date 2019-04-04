using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class JsonTests
    {
        [Test]
        public void Does_escape_strings_in_JSON()
        {
            var json = @"{""app.settings"":""debug true\nname MyApp\n""}";
            var obj = (Dictionary<string,object>)JSON.parse(json);
            
            Assert.That(obj["app.settings"], Is.EqualTo("debug true\nname MyApp\n"));
        }

        [Test]
        public void Can_parse_escaped_json()
        {
            var json = "{\"content\": \"warn(\\n          false,\\n          \\\"props in \\\\\\\"\\\" + (route.path) + \\\"\\\\\\\" is a \\\" + (typeof config) + \\\", \\\" +\\n          \\\"expecting an object, function or boolean.\\\"\\n        );\"}";
            var obj = (Dictionary<string,object>)JSON.parse(json);
            Assert.That(obj.ContainsKey("content"));
        }

        [Test]
        public void Does_throw_on_invalid_number()
        {
            try
            {
                JSON.parse(@"{""test"":23.34.3333}");
                Assert.Fail("should throw");
            }
            catch (FormatException) {}
        }

    }
}