using System;
using System.Runtime.Remoting;
using System.Runtime.Serialization;
using NUnit.Framework;
using ServiceStack.Text;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    [TestFixture]
    public class SoapTests
    {
        [Test]
        public void Does_call_Hello_Service()
        {
            var client = new Soap12ServiceClient(Config.ServiceStackBaseUri);

            var response = client.Send<HelloResponse>(new Hello { Name = "World" });

            Assert.That(response.Result, Is.EqualTo("Hello, World"));
        }

        [Test]
        public void Soap12_Service_does_not_return_BOM()
        {
            var soapBody = @"
<s:Envelope xmlns:s=""http://www.w3.org/2003/05/soap-envelope"" 
xmlns:a=""http://www.w3.org/2005/08/addressing"">
<s:Header>
    <a:Action s:mustUnderstand=""1"">Hello</a:Action>
    <a:MessageID>urn:uuid:be5fddae-9fa3-4d5f-bc31-6ee4980d74f1</a:MessageID>
    <a:ReplyTo><a:Address>http://www.w3.org/2005/08/addressing/anonymous</a:Address></a:ReplyTo>
    <a:To s:mustUnderstand=""1"">http://macbook/api/Soap12</a:To>
</s:Header>
<s:Body>
    <Hello xmlns=""http://schemas.servicestack.net/types"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"">
        <Name>World</Name>
    </Hello>
</s:Body>
</s:Envelope>";

            var url = Config.ServiceStackBaseUri.AppendPath("Soap12");
            var soapBytes = url.PostBytesToUrl(requestBody: soapBody.ToUtf8Bytes(),
                contentType: "application/soap+xml; charset=utf-8");

            //http://en.wikipedia.org/wiki/Byte_order_mark
            //BitConverter.ToString(soapBytes).Print(); //print hex

            byte firstByte = soapBytes[0];
            Assert.That(firstByte, Is.Not.EqualTo(239)); //UTF-8 BOM
            Assert.That(firstByte, Is.EqualTo((int)'<'));

            var soapXml = soapBytes.FromUtf8Bytes();

            Assert.That(soapXml, Does.StartWith(@"<?xml version=""1.0"" encoding=""utf-8""?><s:Envelope"));
        }

        [Test]
        public void Can_send_SoapRequest()
        {
            var client = new Soap12ServiceClient(Config.ServiceStackBaseUri);
//            var client = new Soap12ServiceClient("http://test.servicestack.net");
            
            var request = new GetStuff {
                SummaryDate = DateTime.Parse("2018-04-26"),
                SummaryEndDate = DateTime.Parse("2018-04-26"),
                Symbol = "TOU",
                Email = "Guy.Smiley@myCompany.net",
                IsEnabled = true,
            };

            var response = client.Send(request);
            
            Assert.That(response.SummaryDate, Is.EqualTo(request.SummaryDate));
            Assert.That(response.SummaryEndDate, Is.EqualTo(request.SummaryEndDate));
            Assert.That(response.Symbol, Is.EqualTo(request.Symbol));
            Assert.That(response.Email, Is.EqualTo(request.Email));
            Assert.That(response.IsEnabled, Is.EqualTo(request.IsEnabled));
        }
    }
    
    [DataContract(Namespace = "http://schemas.servicestack.net/types")]
    [Route("/Stuff")]
    public class GetStuff : IReturn<GetStuffResponse>
    {
        [DataMember]
        [ApiMember(Name = "Summary Date",
            DataType = "DateTime",
            IsRequired = false)]
        public DateTime? SummaryDate { get; set; }

        [DataMember]
        [ApiMember(Name = "Summary End Date",
            DataType = "DateTime",
            IsRequired = false)]
        public DateTime? SummaryEndDate { get; set; }

        [DataMember]
        [ApiMember(Name = "Symbol",
            DataType = "string",
            IsRequired = false)]
        public string Symbol { get; set; }

        [DataMember]
        [ApiMember(Name = "Email",
            DataType = "string",
            IsRequired = false)]
        public string Email { get; set; }

        [DataMember]
        [ApiMember(Name = "Is Enabled",
            DataType = "bool",
            IsRequired = false)]
        public bool? IsEnabled { get; set; }
    }

    [DataContract(Namespace = "http://schemas.servicestack.net/types")]
    public class GetStuffResponse
    {
        [DataMember]
        public DateTime? SummaryDate { get; set; }
        [DataMember]
        public DateTime? SummaryEndDate { get; set; }
        [DataMember]
        public string Symbol { get; set; }
        [DataMember]
        public string Email { get; set; }
        [DataMember]
        public bool? IsEnabled { get; set; }
    }

    public class SoapTestServices : Service
    {
        public object Any(GetStuff request) => request.ConvertTo<GetStuffResponse>();
    }
}