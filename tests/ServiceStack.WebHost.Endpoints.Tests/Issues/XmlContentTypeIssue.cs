using System.Reflection;
using System.Runtime.Serialization;
using Funq;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.Issues
{

    [Route("/testxml", Verbs = "POST")]
    [DataContract(Namespace = "")]
    public class TestXml
    {
        [DataMember(Order = 0)]
        public string User { get; set; }
    }

    public class SimpleXmlService : Service
    {
        public object Any(TestXml request)
        {
            return request;
        }
    }

    [TestFixture]
    public class XmlContentTypeIssue
    {
         class AppHost : AppSelfHostBase
         {
             public AppHost()
                 : base(typeof(XmlContentTypeIssue).Name, typeof(XmlContentTypeIssue).Assembly) {}

             public override void Configure(Container container)
             {                 
             }
         }

        private ServiceStackHost appHost;

        public XmlContentTypeIssue()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.AbsoluteBaseUri);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Can_Post_Xml_with_Utf8_charset()
        {
            var xml = @"<TestXml>
                          <User>steve</User>
                        </TestXml>";
            var response = Config.AbsoluteBaseUri.CombineWith("/testxml")
                  .PostStringToUrl(xml, contentType: "text/xml; charset=utf-8", accept: "application/json");

            Assert.That(response, Is.EqualTo("{\"User\":\"steve\"}"));
        }
    }
}