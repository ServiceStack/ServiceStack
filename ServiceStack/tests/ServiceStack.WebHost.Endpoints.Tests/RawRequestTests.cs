using System.IO;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [Route("/rawbytesrequest")]
    public class RawBytesRequest : IRequiresRequestStream
    {
        public Stream RequestStream { get; set; }
    }

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

    [Route("/rawsvg/{Letter}", "GET")]
    public class RawSvg
    {
        public string Letter { get; set; }
    }

    public class RawRequestService : Service
    {
        public async Task<object> Any(RawBytesRequest request)
        {
            var rawRequest = await request.RequestStream.ReadFullyAsync();
            return new RawRequestResponse { Result = rawRequest.FromUtf8Bytes() };
        }

        public async Task<object> Any(RawRequest request)
        {
            var rawRequest = await request.RequestStream.ReadToEndAsync();
            return new RawRequestResponse { Result = rawRequest };
        }

        public async Task<object> Any(RawRequestWithParam request)
        {
            var rawRequest = await request.RequestStream.ReadToEndAsync();
            return new RawRequestResponse { Result = request.Path + ":" + request.Param + ":" + rawRequest };
        }

        public async Task<object> Any(CustomXml request)
        {
            var xml = await request.RequestStream.ReadToEndAsync();
            return xml;
        }

        private const string SvgTemplate = @"<svg width=""100"" height=""100"" xmlns=""http://www.w3.org/2000/svg"">
 <g>
  <rect x=""0"" y=""0"" width=""100"" height=""100"" id=""canvas_background"" fill=""#999999""/>
 </g>
 <g>
  <text x=""50%"" y=""60%"" alignment-baseline=""middle"" text-anchor=""middle"" fill=""#ffffff"" font-size=""80"" font-family=""Helvetica, Arial, sans-serif"" font-weight=""bold"">LETTER</text>
 </g>
</svg>";

        public object Get(RawSvg request)
        {
            Response.ContentType = MimeTypes.GetMimeType("svg");
            return SvgTemplate.Replace("LETTER", (request.Letter ?? "A").Substring(0, 1).ToUpper());
        }
    }

    [TestFixture]
    public class RawRequestTests
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost() 
                : base(nameof(RawRequestTests), typeof(RawRequestService).Assembly) {}

            public override void Configure(Container container)
            {
            }
        }

        private readonly ServiceStackHost appHost;
        public RawRequestTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

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
        public void Can_POST_raw_request_to_predefined_route()
        {
            var rawData = "{\"raw\":\"json\"}";
            var requestUrl = Config.ServiceStackBaseUri + "/json/reply/RawRequest";
            var json = requestUrl.PostJsonToUrl(rawData);
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

        [Test]
        public void Can_download_svg()
        {
            var requestUrl = Config.ServiceStackBaseUri + "/rawsvg/M";
            var svg = requestUrl.GetStringFromUrl(
                accept: "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8",
                responseFilter: res => Assert.That(res.GetHeader(HttpHeaders.ContentType), Does.StartWith(MimeTypes.ImageSvg)));

            Assert.That(svg, Does.Contain(">M</text>"));
        }

    }

}