using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.Host;
using ServiceStack.Text;
using ServiceStack.Web;
using ServiceStack.WebHost.Endpoints.Tests.Support;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [DataContract]
    [Route("/secure")]
    public class Secure
    {
        [DataMember]
        public string UserName { get; set; }
    }

    [DataContract]
    public class SecureResponse : IHasResponseStatus
    {
        [DataMember]
        public string Result { get; set; }

        [DataMember]
        public ResponseStatus ResponseStatus { get; set; }
    }

    public class SecureService : IService
    {
        public object Any(Secure request)
        {
            return new SecureResponse { Result = "Confidential" };
        }
    }

    [DataContract]
    [Route("/insecure")]
    public class Insecure
    {
        [DataMember]
        public string UserName { get; set; }
    }

    [DataContract]
    public class InsecureResponse : IHasResponseStatus
    {
        [DataMember]
        public string Result { get; set; }

        [DataMember]
        public ResponseStatus ResponseStatus { get; set; }
    }

    public class InsecureService : IService
    {
        public object Any(Insecure request)
        {
            return new InsecureResponse { Result = "Public" };
        }
    }

    [DataContract(Namespace = HostConfig.DefaultWsdlNamespace)]
    public class SoapDeserializationException : IReturn<SoapDeserializationExceptionResponse> { }

    [DataContract(Namespace = HostConfig.DefaultWsdlNamespace)]
    public class SoapDeserializationExceptionResponse
    {
        [DataMember(EmitDefaultValue = false, IsRequired = true)]
        public string RequiredProperty { get; set; }

        [DataMember]
        public ResponseStatus ResponseStatus { get; set; }
    }

    [DataContract(Namespace = HostConfig.DefaultWsdlNamespace)]
    public class SoapFaultTest : IReturn<SoapFaultTestResponse> { }

    [DataContract(Namespace = HostConfig.DefaultWsdlNamespace)]
    public class SoapFaultTestResponse
    {
        [DataMember]
        public ResponseStatus ResponseStatus { get; set; }
    }

    public class SoapServices : Service
    {
        public object Any(SoapDeserializationException request)
        {
            return new SoapDeserializationExceptionResponse();
        }

        public object Any(SoapFaultTest request)
        {
            throw new Exception("Test SOAP Fault");
        }
    }

    public class ThrowsInFilterAttribute : RequestFilterAttribute
    {
        public override void Execute(IRequest req, IResponse res, object requestDto)
        {
            var dto = (ThrowsInFilter)requestDto;

            var status = (HttpStatusCode)dto.StatusCode.GetValueOrDefault(400);
            var errorMsg = dto.Message ?? nameof(ThrowsInFilter);

            if (dto.ExceptionType == nameof(HttpError))
                throw new HttpError(status, errorMsg);

            throw new Exception(errorMsg);
        }
    }

    public class ThrowsInFilter : IReturn<ThrowsInFilter>
    {
        public string ExceptionType { get; set; }
        public string Message { get; set; }
        public int? StatusCode { get; set; }
    }

    [ThrowsInFilter]
    public class ThrowsInFilterService : Service
    {
        public object Any(ThrowsInFilter request) => request;
    }

    [TestFixture]
    public abstract class RequestFiltersTests
    {
        private const string AllowedUser = "user";
        private const string AllowedPass = "p@55word";

        public class RequestFiltersAppHostHttpListener
            : AppHostHttpListenerBase
        {
            private Guid currentSessionGuid;

            public RequestFiltersAppHostHttpListener()
                : base("Request Filters Tests", typeof(GetFactorialService).Assembly) { }

            public override void Configure(Container container)
            {
#if !NETCORE
                Plugins.Add(new SoapFormat());
#endif

                this.GlobalRequestFilters.Add((req, res, dto) =>
                {
                    var userPass = req.GetBasicAuthUserAndPassword();
                    if (userPass == null)
                    {
                        return;
                    }

                    var userName = userPass.Value.Key;
                    if (userName == AllowedUser && userPass.Value.Value == AllowedPass)
                    {
                        currentSessionGuid = Guid.NewGuid();
                        var sessionKey = userName + "/" + currentSessionGuid.ToString("N");

                        //set session for this request (as no cookies will be set on this request)
                        req.Items["ss-session"] = sessionKey;
                        res.SetPermanentCookie("ss-session", sessionKey);
                    }
                });
                this.GlobalRequestFilters.Add((req, res, dto) =>
                {
                    if (dto is Secure)
                    {
                        var sessionId = req.GetItemOrCookie("ss-session") ?? string.Empty;
                        var sessionIdParts = sessionId.SplitOnFirst('/');
                        if (sessionIdParts.Length < 2 || sessionIdParts[0] != AllowedUser || sessionIdParts[1] != currentSessionGuid.ToString("N"))
                        {
                            res.ReturnAuthRequired();
                        }
                    }
                    if (dto is SoapDeserializationException)
                    {
                        req.Response.UseBufferedStream = true;
                    }
                });
#if !NETCORE
                this.ServiceExceptionHandlers.Add((req, dto, ex) =>
                {
                    if (dto is SoapDeserializationException)
                        return new SoapDeserializationExceptionResponse { RequiredProperty = "ServiceExceptionHandlers" };

                    if (dto is SoapFaultTest)
                    {
                        if (req.GetItem(Keywords.SoapMessage) is System.ServiceModel.Channels.Message requestMsg)
                        {
                            var msgVersion = requestMsg.Version;
                            using (var response = System.Xml.XmlWriter.Create(req.Response.OutputStream))
                            {
                                var message = System.ServiceModel.Channels.Message.CreateMessage(
                                    msgVersion, new System.ServiceModel.FaultCode("Receiver"), ex.Message, null);
                                message.WriteMessage(response);
                            }
                            req.Response.End();
                        }
                    }

                    return null;
                });
#endif
            }
        }

        RequestFiltersAppHostHttpListener appHost;

        string ServiceClientBaseUri = Config.ListeningOn;

        [OneTimeSetUp]
        public void OnTestFixtureSetUp()
        {
            appHost = new RequestFiltersAppHostHttpListener();
            appHost.Init();
            appHost.Start(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void OnTestFixtureTearDown()
        {
            appHost.Dispose();
        }

        protected abstract IServiceClient CreateNewServiceClient();
        protected abstract IRestClientAsync CreateNewRestClientAsync();

        protected virtual string GetFormat()
        {
            return null;
        }

        private static void Assert401(IServiceClient client, WebServiceException ex)
        {
#if !NETCORE
            if (client is Soap11ServiceClient || client is Soap12ServiceClient)
            {
                if (ex.StatusCode != 401)
                {
                    Console.WriteLine("WARNING: SOAP clients returning 500 instead of 401");
                }
                return;
            }
#endif
            Console.WriteLine(ex);
            Assert.That(ex.StatusCode, Is.EqualTo(401));
        }

        private static void FailOnAsyncError<T>(T response, Exception ex)
        {
            Assert.Fail(ex.Message);
        }

        private static bool Assert401(Exception ex)
        {
            var webEx = (WebServiceException)ex;
            Assert.That(webEx.StatusCode, Is.EqualTo(401));
            return true;
        }

        [Test]
        public void Can_login_with_Basic_auth_to_access_Secure_service()
        {
            var format = GetFormat();
            if (format == null) return;

            var req = (HttpWebRequest)WebRequest.Create(ServiceClientBaseUri.CombineWith("{0}/reply/Secure".Fmt(format)));

            req.Headers[HttpHeaders.Authorization]
                = "basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(AllowedUser + ":" + AllowedPass));

            var dtoString = new StreamReader(req.GetResponse().GetResponseStream()).ReadToEnd();
            Assert.That(dtoString.Contains("Confidential"));
            Console.WriteLine(dtoString);
        }

        [Test]
        public void Can_login_with_Basic_auth_to_access_Secure_service_using_ServiceClient()
        {
            var format = GetFormat();
            if (format == null) return;

            var client = CreateNewServiceClient();
            client.SetCredentials(AllowedUser, AllowedPass);

            var response = client.Send<SecureResponse>(new Secure());

            Assert.That(response.Result, Is.EqualTo("Confidential"));
        }

        [Test]
        public async Task Can_login_with_Basic_auth_to_access_Secure_service_using_RestClientAsync()
        {
            var format = GetFormat();
            if (format == null) return;

            var client = CreateNewRestClientAsync();
            client.SetCredentials(AllowedUser, AllowedPass);

            var response = await client.GetAsync<SecureResponse>(ServiceClientBaseUri + "secure");

            Assert.That(response.Result, Is.EqualTo("Confidential"));
        }

        [Test]
        public void Can_login_without_authorization_to_access_Insecure_service()
        {
            var format = GetFormat();
            if (format == null) return;

            var req = (HttpWebRequest)WebRequest.Create($"{ServiceClientBaseUri}{format}/reply/Insecure");

            req.Headers[HttpHeaders.Authorization]
                = "basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(AllowedUser + ":" + AllowedPass));

            var dtoString = new StreamReader(req.GetResponse().GetResponseStream()).ReadToEnd();
            Assert.That(dtoString.Contains("Public"));
            Console.WriteLine(dtoString);
        }

        [Test]
        public void Can_login_without_authorization_to_access_Insecure_service_using_ServiceClient()
        {
            var format = GetFormat();
            if (format == null) return;

            var client = CreateNewServiceClient();

            var response = client.Send<InsecureResponse>(new Insecure());

            Assert.That(response.Result, Is.EqualTo("Public"));
        }

        [Test]
        public async Task Can_login_without_authorization_to_access_Insecure_service_using_RestClientAsync()
        {
            var format = GetFormat();
            if (format == null) return;

            var client = CreateNewRestClientAsync();

            var response = await client.GetAsync<InsecureResponse>(ServiceClientBaseUri + "insecure");

            Assert.That(response.Result, Is.EqualTo("Public"));
        }

        [Test]
        public void Can_login_with_session_cookie_to_access_Secure_service()
        {
            var format = GetFormat();
            if (format == null) return;

            var req = (HttpWebRequest)WebRequest.Create(ServiceClientBaseUri.CombineWith("{0}/reply/Secure".Fmt(format)));

            req.Headers[HttpHeaders.Authorization]
                = "basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(AllowedUser + ":" + AllowedPass));

            var res = (HttpWebResponse)req.GetResponse();
            var cookie = res.Cookies["ss-session"];
            if (cookie != null)
            {
                req = (HttpWebRequest)WebRequest.Create(ServiceClientBaseUri.CombineWith("{0}/reply/Secure".Fmt(format)));
                req.CookieContainer.Add(new Uri(ServiceClientBaseUri), new Cookie("ss-session", cookie.Value));

                var dtoString = new StreamReader(req.GetResponse().GetResponseStream()).ReadToEnd();
                Assert.That(dtoString.Contains("Confidential"));
                Console.WriteLine(dtoString);
            }
        }

        [Test]
        public void Get_401_When_accessing_Secure_using_fake_sessionid_cookie()
        {
            var format = GetFormat();
            if (format == null) return;

            var req = (HttpWebRequest)WebRequest.Create(ServiceClientBaseUri.CombineWith("{0}/reply/Secure".Fmt(format)));

            req.CookieContainer = new CookieContainer();
            req.CookieContainer.Add(new Uri("http://localhost"), new Cookie("ss-session", AllowedUser + "/" + Guid.NewGuid().ToString("N"), "/", "localhost"));

            try
            {
                var res = req.GetResponse();
            }
            catch (WebException x)
            {
                Assert.That(((HttpWebResponse)x.Response).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            }
        }

        [Test]
        public void Get_401_When_accessing_Secure_using_ServiceClient_without_Authorization()
        {
            var client = CreateNewServiceClient();

            try
            {
                var response = client.Send<SecureResponse>(new Secure());
                Console.WriteLine(response.Dump());
            }
            catch (WebServiceException ex)
            {
                Assert401(client, ex);
                return;
            }
            Assert.Fail("Should throw WebServiceException.StatusCode == 401");
        }

        [Test]
        public async Task Does_populate_ResponseStatus_when_Exception_thrown_in_RequestFilter()
        {
            var client = CreateNewRestClientAsync();
            if (client == null) return;

            try
            {
                var response = await client.PostAsync(new ThrowsInFilter
                {
                    StatusCode = 409,
                    ExceptionType = nameof(HttpError),
                    Message = "POST HttpError CONFLICT",
                });

                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(409));
                Assert.That(ex.ResponseStatus.ErrorCode, Is.EqualTo("Conflict"));
                Assert.That(ex.ResponseStatus.Message, Is.EqualTo("POST HttpError CONFLICT"));
            }

            try
            {
                var response = await client.PostAsync(new ThrowsInFilter
                {
                    ExceptionType = nameof(Exception),
                    Message = "POST Generic Exception",
                });

                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(500));
                Assert.That(ex.ResponseStatus.ErrorCode, Is.EqualTo("Exception"));
                Assert.That(ex.ResponseStatus.Message, Is.EqualTo("POST Generic Exception"));
            }
        }

        [Test]
        public async Task Get_401_When_accessing_Secure_using_RestClient_GET_without_Authorization()
        {
            var client = CreateNewRestClientAsync();
            if (client == null) return;

            try
            {
                await client.GetAsync<SecureResponse>(ServiceClientBaseUri + "secure");
                Assert.Fail("Should throw WebServiceException.StatusCode == 401");
            }
            catch (WebServiceException webEx)
            {
                Assert401(webEx);
                Assert.That(webEx.ResponseDto, Is.Null);
            }
        }

        [Test]
        public async Task Get_401_When_accessing_Secure_using_RestClient_DELETE_without_Authorization()
        {
            var client = CreateNewRestClientAsync();
            if (client == null) return;

            try
            {
                await client.DeleteAsync<SecureResponse>(ServiceClientBaseUri + "secure");
                Assert.Fail("Should throw WebServiceException.StatusCode == 401");
            }
            catch (WebServiceException webEx)
            {
                Assert401(webEx);
                Assert.That(webEx.ResponseDto, Is.Null);
            }
        }

        [Test]
        public async Task Get_401_When_accessing_Secure_using_RestClient_POST_without_Authorization()
        {
            var client = CreateNewRestClientAsync();
            if (client == null) return;

            try
            {
                await client.PostAsync<SecureResponse>(ServiceClientBaseUri + "secure", new Secure());
                Assert.Fail("Should throw WebServiceException.StatusCode == 401");
            }
            catch (WebServiceException webEx)
            {
                Assert401(webEx);
                Assert.That(webEx.ResponseDto, Is.Null);
            }
        }

        [Test]
        public async Task Get_401_When_accessing_Secure_using_RestClient_PUT_without_Authorization()
        {
            var client = CreateNewRestClientAsync();
            if (client == null) return;

            try
            {
                await client.PutAsync<SecureResponse>(ServiceClientBaseUri + "secure", new Secure());
                Assert.Fail("Should throw WebServiceException.StatusCode == 401");
            }
            catch (WebServiceException webEx)
            {
                Assert401(webEx);
                Assert.That(webEx.ResponseDto, Is.Null);
            }
        }

        public class UnitTests : RequestFiltersTests
        {
            protected override IServiceClient CreateNewServiceClient()
            {
                return new DirectServiceClient(appHost.ServiceController);
            }

            protected override IRestClientAsync CreateNewRestClientAsync()
            {
                return null; //TODO implement REST calls with DirectServiceClient (i.e. Unit Tests)
                //EndpointHandlerBase.ServiceManager = new ServiceManager(true, typeof(SecureService).Assembly);
                //return new DirectServiceClient(EndpointHandlerBase.ServiceManager);
            }
        }

        public class XmlIntegrationTests : RequestFiltersTests
        {
            protected override string GetFormat()
            {
                return "xml";
            }

            protected override IServiceClient CreateNewServiceClient()
            {
                return new XmlServiceClient(ServiceClientBaseUri);
            }

            protected override IRestClientAsync CreateNewRestClientAsync()
            {
                return new XmlServiceClient(ServiceClientBaseUri);
            }
        }

        [TestFixture]
        public class JsonIntegrationTests : RequestFiltersTests
        {
            protected override string GetFormat()
            {
                return "json";
            }

            protected override IServiceClient CreateNewServiceClient()
            {
                return new JsonServiceClient(ServiceClientBaseUri);
            }

            protected override IRestClientAsync CreateNewRestClientAsync()
            {
                return new JsonServiceClient(ServiceClientBaseUri);
            }
        }

        [TestFixture]
        public class JsvIntegrationTests : RequestFiltersTests
        {
            protected override string GetFormat()
            {
                return "jsv";
            }

            protected override IServiceClient CreateNewServiceClient()
            {
                return new JsvServiceClient(ServiceClientBaseUri);
            }

            protected override IRestClientAsync CreateNewRestClientAsync()
            {
                return new JsvServiceClient(ServiceClientBaseUri);
            }
        }

#if !NETCORE

        [TestFixture]
        public class Soap11IntegrationTests : RequestFiltersTests
        {
            protected override IServiceClient CreateNewServiceClient()
            {
                return new Soap11ServiceClient(ServiceClientBaseUri);
            }

            protected override IRestClientAsync CreateNewRestClientAsync()
            {
                return null;
            }
        }

        [TestFixture]
        public class Soap12IntegrationTests : RequestFiltersTests
        {
            protected override IServiceClient CreateNewServiceClient()
            {
                return new Soap12ServiceClient(ServiceClientBaseUri);
            }

            protected override IRestClientAsync CreateNewRestClientAsync()
            {
                return null;
            }

            [Test]
            public void Response_serialization_errors_does_call_ServiceExceptionHandlers()
            {
                var client = CreateNewServiceClient();
                var response = client.Send(new SoapDeserializationException());
                Assert.That(response.RequiredProperty, Is.EqualTo("ServiceExceptionHandlers"));
            }

            [Test]
            public void Does_return_Custom_SoapFault()
            {
                var responseSoap = ServiceClientBaseUri.CombineWith("/Soap12")
                    .SendStringToUrl(method: "POST", 
                        requestBody: @"<s:Envelope xmlns:s=""http://www.w3.org/2003/05/soap-envelope"" xmlns:a=""http://www.w3.org/2005/08/addressing"">
                            <s:Header>
                                <a:Action s:mustUnderstand=""1"">SoapFaultTest</a:Action>
                                <a:MessageID>urn:uuid:84d9f946-d3c3-4252-84ff-0b306ce53386</a:MessageID>
                                <a:ReplyTo><a:Address>http://www.w3.org/2005/08/addressing/anonymous</a:Address>
                                </a:ReplyTo>
                                <a:To s:mustUnderstand=""1"">http://localhost:20000/Soap12</a:To>
                            </s:Header>
                            <s:Body>
                                <SoapFaultTest xmlns=""http://schemas.servicestack.net/types"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""/>
                            </s:Body>
                        </s:Envelope>", 
                        contentType: "application/soap+xml; charset=utf-8",
                        responseFilter: res => { });

                Assert.That(responseSoap, Is.EqualTo(
                    @"<?xml version=""1.0"" encoding=""utf-8""?><s:Envelope xmlns:s=""http://www.w3.org/2003/05/soap-envelope""><s:Body><s:Fault><s:Code><s:Value>s:Receiver</s:Value></s:Code><s:Reason><s:Text xml:lang=""en-US"">Test SOAP Fault</s:Text></s:Reason></s:Fault></s:Body></s:Envelope>"));
            }

        }

#endif

    }
}