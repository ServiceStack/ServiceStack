using System.IO;
using NUnit.Framework;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    [Route("/rawrequest")]
    public class RawRequest : IRequiresRequestStream
    {
        public Stream RequestStream { get; set; }
    }

    [Route("/rawrequest/{Path}")]
    public class RawRequestWithParam : IRequiresRequestStream
    {
        public string Path { get; set; }
        public string Param { get; set; }
        public Stream RequestStream { get; set; }
    }

    public class RawRequestResponse
    {
        public string Result { get; set; }
    }

    [Restrict(RequestAttributes.Xml)]
    [Route("/Leads/LeadData/", "POST", Notes = "LMS - DirectApi")]
    public class CustomXml : IRequiresRequestStream
    {
        public Stream RequestStream { get; set; }
    }

    public class RawRequestService : IService
    {
        public object Any(RawRequest request)
        {
            var rawRequest = request.RequestStream.ToUtf8String();
            return new RawRequestResponse { Result = rawRequest };
        }

        public object Any(RawRequestWithParam request)
        {
            var rawRequest = request.RequestStream.ToUtf8String();
            return new RawRequestResponse { Result = request.Path + ":" + request.Param + ":" + rawRequest };
        }

        public object Any(CustomXml request)
        {
            var xml = request.RequestStream.ReadFully().FromUtf8Bytes();
            return xml;
        }
    }

    [TestFixture]
    public class RawRequestTests
    {
        [Test]
        public void Can_POST_raw_request()
        {
            var rawData = "<<(( 'RAW_DATA' ))>>";
            var requestUrl = Config.ServiceStackBaseUri + "/rawrequest";
            var json = requestUrl.PostStringToUrl(rawData, contentType: MimeTypes.PlainText, accept: MimeTypes.Json);
            var response = json.FromJson<RawRequestResponse>();
            Assert.That(response.Result, Is.EqualTo(rawData));
        }

        [Test]
        public void Can_POST_raw_request_with_params()
        {
            var rawData = "<<(( 'RAW_DATA' ))>>";
            var requestUrl = Config.ServiceStackBaseUri + "/rawrequest/Foo?Param=Bar";
            var json = requestUrl.PostStringToUrl(rawData, contentType: MimeTypes.PlainText, accept: MimeTypes.Json);
            var response = json.FromJson<RawRequestResponse>();
            var expected = "{0}:{1}:{2}".Fmt("Foo", "Bar", rawData);
            Assert.That(response.Result, Is.EqualTo(expected));
        }

        [Test]
        public void Can_PUT_raw_request()
        {
            var rawData = "<<(( 'RAW_DATA' ))>>";
            var requestUrl = Config.ServiceStackBaseUri + "/rawrequest";
            var json = requestUrl.PutStringToUrl(rawData, contentType: MimeTypes.PlainText, accept: MimeTypes.Json);
            var response = json.FromJson<RawRequestResponse>();
            Assert.That(response.Result, Is.EqualTo(rawData));
        }

        [Test]
        public void Can_POST_Custom_XML()
        {
            var xml = @"<LeadApplications>
                          <LeadApplication>
                            <Email>daffy.duck@example.com</Email>
                            <FirstName>Joey</FirstName>
                            <MiddleName>Disney</MiddleName>
                            <LastName>Duck</LastName>
                            <Street1>1 Disneyland Street</Street1>
                            <Street2>2 Disneyland Street</Street2>
                            <City>PAUMA VALLEY</City>
                            <State>CA</State>   
                            <Zip>92503</Zip>
                          </LeadApplication>
                        </LeadApplications>";

            var requestUrl = Config.ServiceStackBaseUri + "/Leads/LeadData/";
            var responseXml = requestUrl.PostXmlToUrl(xml);

            Assert.That(responseXml, Is.EqualTo(xml));
        }
    }

}