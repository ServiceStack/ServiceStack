using System.Collections.Generic;
using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests.TemplateTests
{
    public class TemplateJsonTests
    {
        [Test]
        public void Does_escape_strings_in_JSON()
        {
            var json = @"{""app.settings"":""debug true\nname MyApp\n""}";
            var obj = (Dictionary<string,object>)JSON.parse(json);
            
            Assert.That(obj["app.settings"], Is.EqualTo("debug true\nname MyApp\n"));
        }
    }
}