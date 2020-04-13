using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Serialization;
using System.ServiceModel.Channels;
using System.Xml;
using NUnit.Framework;
using ServiceStack.Common.Tests;
using ServiceStack.Text;
using ServiceStack.Validation;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    /// <summary>
    /// This base class executes all Web Services ignorant of the endpoints its hosted on.
    /// The same tests below are re-used by the Unit and Integration TestFixture's declared below
    /// </summary>
    [TestFixture]
    public abstract class WebServicesTests
        : TestBase
    {
        private const string TestString = "ServiceStack";

        protected WebServicesTests()
            : base(Config.ServiceStackBaseUri, typeof(ReverseService).Assembly)
        {
        }

        protected override void Configure(Funq.Container container) { }

        [Test]
        public void Does_Execute_ReverseService()
        {
            var client = CreateNewServiceClient();
            var response = client.Send<ReverseResponse>(
                new Reverse { Value = TestString });

            var expectedValue = ReverseService.Execute(TestString);
            Assert.That(response.Result, Is.EqualTo(expectedValue));
        }

        [Test]
        public void Does_Execute_Rot13Service()
        {
            var client = CreateNewServiceClient();
            var response = client.Send<Rot13Response>(new Rot13 { Value = TestString });

            var expectedValue = TestString.ToRot13();
            Assert.That(response.Result, Is.EqualTo(expectedValue));
        }

        [Test]
        public void Does_Execute_AddInts()
        {
            var client = CreateNewServiceClient();
            var response = client.Send<AddIntsResponse>(new AddInts { A = 1, B = 2 });

            Assert.That(response.Result, Is.EqualTo(3));
        }

        [Test]
        public void Can_Handle_Exception_from_AlwaysThrowService()
        {
            var client = CreateNewServiceClient();
            try
            {
                var response = client.Send<AlwaysThrowsResponse>(
                    new AlwaysThrows { Value = TestString });

                Assert.Fail("Should throw HTTP errors");
            }
            catch (WebServiceException webEx)
            {
                var response = (AlwaysThrowsResponse)webEx.ResponseDto;
                var expectedError = AlwaysThrowsService.GetErrorMessage(TestString);
                Assert.That(response.ResponseStatus.ErrorCode,
                    Is.EqualTo(typeof(NotImplementedException).Name));
                Assert.That(response.ResponseStatus.Message,
                    Is.EqualTo(expectedError));
            }
        }

        [Test]
        public void Can_Handle_Exception_from_AlwaysThrowsList_with_GET_route()
        {
            var client = CreateNewServiceClient();
            if (client is WcfServiceClient) return;
            try
            {
                var response = client.Get<List<AlwaysThrows>>("/throwslist/404/{0}".Fmt(TestString));

                Assert.Fail("Should throw HTTP errors");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo(404));

                var response = (ErrorResponse)webEx.ResponseDto;
                var expectedError = AlwaysThrowsService.GetErrorMessage(TestString);
                Assert.That(response.ResponseStatus.ErrorCode,
                    Is.EqualTo(typeof(NotImplementedException).Name));
                Assert.That(response.ResponseStatus.Message,
                    Is.EqualTo(expectedError));
            }
        }

        [Test]
        public void Can_Handle_Exception_from_AlwaysThrowsValidation()
        {
            var client = CreateNewServiceClient();
            try
            {
                var response = client.Send<List<AlwaysThrows>>(
                    new AlwaysThrowsValidation());

                Assert.Fail("Should throw HTTP errors");
            }
            catch (WebServiceException webEx)
            {
                var response = (ErrorResponse)webEx.ResponseDto;
                var status = response.ResponseStatus;
                Assert.That(status.ErrorCode, Is.EqualTo("NotEmpty"));
                Assert.That(status.Message, Is.EqualTo("'Value' must not be empty."));
                Assert.That(status.Errors[0].ErrorCode, Is.EqualTo("NotEmpty"));
                Assert.That(status.Errors[0].FieldName, Is.EqualTo("Value"));
                Assert.That(status.Errors[0].Message, Is.EqualTo("'Value' must not be empty."));
            }
        }

        [Test]
        public void Request_items_are_preserved_between_filters()
        {
            var client = CreateNewServiceClient();
            if (client is DirectServiceClient) return;
            var response = client.Send<RequestItemsResponse>(new RequestItems { });
            Assert.That(response.Result, Is.EqualTo("MissionSuccess"));
        }
    }


    /// <summary>
    /// Unit tests simulates an in-process ServiceStack host where all services
    /// are executed all in-memory so DTO's are not even serialized.
    /// </summary>
    public class UnitTests : WebServicesTests
    {
        public UnitTests()
        {
            AppHost.Container.RegisterValidators(typeof(AlwaysThrowsValidator).Assembly);
            AppHost.LoadPlugin(new ValidationFeature());
        }

        protected override IServiceClient CreateNewServiceClient()
        {
            return new DirectServiceClient(this, AppHost.ServiceController);
        }
    }

    public class XmlIntegrationTests : WebServicesTests
    {
        protected override IServiceClient CreateNewServiceClient()
        {
            return new XmlServiceClient(ServiceClientBaseUri);
        }
    }

    public class JsonIntegrationTests : WebServicesTests
    {
        protected override IServiceClient CreateNewServiceClient()
        {
            return new JsonServiceClient(ServiceClientBaseUri);
        }
    }

    public class JsvIntegrationTests : WebServicesTests
    {
        protected override IServiceClient CreateNewServiceClient()
        {
            return new JsvServiceClient(ServiceClientBaseUri);
        }
    }

    public class Soap11IntegrationTests : WebServicesTests
    {
        protected override IServiceClient CreateNewServiceClient()
        {
            return new Soap11ServiceClient(ServiceClientBaseUri);
        }
    }

    public class Soap12IntegrationTests : WebServicesTests
    {
        protected override IServiceClient CreateNewServiceClient()
        {
            return new Soap12ServiceClient(ServiceClientBaseUri);
//            return new Soap12ServiceClient("http://test.servicestack.net");
        }

        [Test]
        public void Call_AddInts()
        {
            var soap = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<s:Envelope xmlns:s=""http://www.w3.org/2003/05/soap-envelope"" xmlns:a=""http://www.w3.org/2005/08/addressing"">
   <s:Header>
      <a:Action s:mustUnderstand=""1"">AddInts</a:Action>
      <a:MessageID>urn:uuid:e6be43e0-c120-4ba8-920e-7ecaa1823fd2</a:MessageID>
      <a:ReplyTo>
         <a:Address>http://www.w3.org/2005/08/addressing/anonymous</a:Address>
      </a:ReplyTo>
      <a:To s:mustUnderstand=""1"">http://localhost:50000/api/Soap12</a:To>
   </s:Header>
   <s:Body>
      <AddInts xmlns=""http://schemas.servicestack.net/types"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"">
         <A>1</A>
         <B>2</B>
      </AddInts>
   </s:Body>
</s:Envelope>";

            var responseXml = ServiceClientBaseUri.CombineWith("/soap12")
                .PostToUrl(soap, requestFilter:req => req.ContentType = "application/soap+xml; charset=utf-8");
            
            responseXml.Print();
            Assert.That(responseXml, Does.Contain("<Result>3</Result>"));
        }

        [Test]
        public void Sending_invalid_request_returns_invalid_response()
        {
            var soap = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<s:Envelope xmlns:s=""http://www.w3.org/2003/05/soap-envelope"" xmlns:a=""http://www.w3.org/2005/08/addressing"">
   <s:Header>
      <a:Action s:mustUnderstand=""1"">AddInts</a:Action>
      <a:MessageID>urn:uuid:e6be43e0-c120-4ba8-920e-7ecaa1823fd2</a:MessageID>
      <a:ReplyTo>
         <a:Address>http://www.w3.org/2005/08/addressing/anonymous</a:Address>
      </a:ReplyTo>
      <a:To s:mustUnderstand=""1"">http://localhost:50000/api/Soap12</a:To>
   </s:Header>
   <s:Body>
      <AddInts xmlns=""http://schemas.servicestack.net/types"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"">
         <A>not a</A>
         <B>number</B>
      </AddInts>
   </s:Body>
</s:Envelope>";

            var responseXml = ServiceClientBaseUri.CombineWith("/soap12")
                .PostToUrl(soap, requestFilter: req => req.ContentType = "application/soap+xml; charset=utf-8");
                
            var doc = new XmlDocument();
            doc.LoadXml(responseXml);

            var responseMsg = Message.CreateMessage(new XmlNodeReader(doc), int.MaxValue,
                MessageVersion.Soap12WSAddressingAugust2004);

            using (var reader = responseMsg.GetReaderAtBodyContents())
            {
                var bodyXml = reader.ReadOuterXml();
                var responseType = typeof(AddIntsResponse);
                var response = (AddIntsResponse)Serialization.DataContractSerializer.Instance.DeserializeFromString(bodyXml, responseType);

                Assert.That(response.ResponseStatus.ErrorCode, Is.EqualTo(nameof(SerializationException)));
                Assert.That(response.ResponseStatus.Message, Does.Contain("Error trying to deserialize requestType:"));
            }
        }
        
    }
}