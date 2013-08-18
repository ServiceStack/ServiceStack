using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using Funq;
using NUnit.Framework;
using ServiceStack.Common.Web;
using ServiceStack.Service;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Support;
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

	public class SecureService : IService<Secure>
	{
		public object Execute(Secure request)
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

	public class InsecureService : IService<Insecure>
	{
		public object Execute(Insecure request)
		{
			return new InsecureResponse { Result = "Public" };
		}
	}

	[TestFixture]
	public abstract class RequestFiltersTests
	{
		private const string ListeningOn = "http://localhost:82/";
		private const string ServiceClientBaseUri = "http://localhost:82/";

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
				this.RequestFilters.Add((req, res, dto) =>
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
				this.RequestFilters.Add((req, res, dto) =>
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
				});
			}
		}

		RequestFiltersAppHostHttpListener appHost;

		[TestFixtureSetUp]
		public void OnTestFixtureSetUp()
		{
			appHost = new RequestFiltersAppHostHttpListener();
			appHost.Init();
			appHost.Start(ListeningOn);
		}

		[TestFixtureTearDown]
		public void OnTestFixtureTearDown()
		{
			appHost.Dispose();
            EndpointHandlerBase.ServiceManager = null;
		}

		protected abstract IServiceClient CreateNewServiceClient();
		protected abstract IRestClientAsync CreateNewRestClientAsync();

		protected virtual string GetFormat()
		{
			return null;
		}

		private static void Assert401(IServiceClient client, WebServiceException ex)
		{
			if (client is Soap11ServiceClient || client is Soap12ServiceClient)
			{
				if (ex.StatusCode != 401)
				{
					Console.WriteLine("WARNING: SOAP clients returning 500 instead of 401");
				}
				return;
			}

			Console.WriteLine(ex);
			Assert.That(ex.StatusCode, Is.EqualTo(401));
		}

		private static void FailOnAsyncError<T>(T response, Exception ex)
		{
			Assert.Fail(ex.Message);
		}

		private static bool Assert401(object response, Exception ex)
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

			var req = (HttpWebRequest)WebRequest.Create(
				string.Format("http://localhost:82/{0}/syncreply/Secure", format));

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
		public void Can_login_with_Basic_auth_to_access_Secure_service_using_RestClientAsync()
		{
			var format = GetFormat();
			if (format == null) return;

			var client = CreateNewRestClientAsync();
			client.SetCredentials(AllowedUser, AllowedPass);

			SecureResponse response = null;
			client.GetAsync<SecureResponse>(ServiceClientBaseUri + "secure",
				r => response = r, FailOnAsyncError);

			Thread.Sleep(2000);
			Assert.That(response.Result, Is.EqualTo("Confidential"));
		}

		[Test]
		public void Can_login_without_authorization_to_access_Insecure_service()
		{
			var format = GetFormat();
			if (format == null) return;

			var req = (HttpWebRequest)WebRequest.Create(
				string.Format("{0}{1}/syncreply/Insecure", ServiceClientBaseUri, format));

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
		public void Can_login_without_authorization_to_access_Insecure_service_using_RestClientAsync()
		{
			var format = GetFormat();
			if (format == null) return;

			var client = CreateNewRestClientAsync();

			InsecureResponse response = null;
			client.GetAsync<InsecureResponse>(ServiceClientBaseUri + "insecure",
				r => response = r, FailOnAsyncError);

			Thread.Sleep(2000);
			Assert.That(response.Result, Is.EqualTo("Public"));
		}

		[Test]
		public void Can_login_with_session_cookie_to_access_Secure_service()
		{
			var format = GetFormat();
			if (format == null) return;

			var req = (HttpWebRequest)WebRequest.Create(
				string.Format("http://localhost:82/{0}/syncreply/Secure", format));

			req.Headers[HttpHeaders.Authorization]
				= "basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(AllowedUser + ":" + AllowedPass));

			var res = (HttpWebResponse)req.GetResponse();
			var cookie = res.Cookies["ss-session"];
			if (cookie != null)
			{
				req = (HttpWebRequest)WebRequest.Create(
					string.Format("http://localhost:82/{0}/syncreply/Secure", format));
				req.CookieContainer.Add(new Cookie("ss-session", cookie.Value));

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

			var req = (HttpWebRequest)WebRequest.Create(
				string.Format("http://localhost:82/{0}/syncreply/Secure", format));

			req.CookieContainer = new CookieContainer();
			req.CookieContainer.Add(new Cookie("ss-session", AllowedUser + "/" + Guid.NewGuid().ToString("N"), "/", "localhost"));

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
		public void Get_401_When_accessing_Secure_using_RestClient_GET_without_Authorization()
		{
			var client = CreateNewRestClientAsync();
			if (client == null) return;

			SecureResponse response = null;
			var wasError = false;
			client.GetAsync<SecureResponse>(ServiceClientBaseUri + "secure",
				r => response = r, (r, ex) => wasError = Assert401(r, ex));

			Thread.Sleep(1000);
			Assert.That(wasError, Is.True,
				"Should throw WebServiceException.StatusCode == 401");
			Assert.IsNull(response);
		}

		[Test]
		public void Get_401_When_accessing_Secure_using_RestClient_DELETE_without_Authorization()
		{
			var client = CreateNewRestClientAsync();
			if (client == null) return;

			SecureResponse response = null;
			var wasError = false;
			client.DeleteAsync<SecureResponse>(ServiceClientBaseUri + "secure",
				r => response = r, (r, ex) => wasError = Assert401(r, ex));

			Thread.Sleep(1000);
			Assert.That(wasError, Is.True,
				"Should throw WebServiceException.StatusCode == 401");
			Assert.IsNull(response);
		}

		[Test]
		public void Get_401_When_accessing_Secure_using_RestClient_POST_without_Authorization()
		{
			var client = CreateNewRestClientAsync();
			if (client == null) return;

			SecureResponse response = null;
			var wasError = false;
			client.PostAsync<SecureResponse>(ServiceClientBaseUri + "secure", new Secure(),
				r => response = r, (r, ex) => wasError = Assert401(r, ex));

			Thread.Sleep(1000);
			Assert.That(wasError, Is.True,
				"Should throw WebServiceException.StatusCode == 401");
			Assert.IsNull(response);
		}

		[Test]
		public void Get_401_When_accessing_Secure_using_RestClient_PUT_without_Authorization()
		{
			var client = CreateNewRestClientAsync();
			if (client == null) return;

			SecureResponse response = null;
			var wasError = false;
			client.PutAsync<SecureResponse>(ServiceClientBaseUri + "secure", new Secure(),
				r => response = r, (r, ex) => wasError = Assert401(r, ex));

			Thread.Sleep(1000);
			Assert.That(wasError, Is.True,
						"Should throw WebServiceException.StatusCode == 401");
			Assert.IsNull(response);
		}


		public class UnitTests : RequestFiltersTests
		{
			protected override IServiceClient CreateNewServiceClient()
			{
                EndpointHandlerBase.ServiceManager = new ServiceManager(typeof(SecureService).Assembly).Init();
				return new DirectServiceClient(EndpointHandlerBase.ServiceManager);
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
				return new XmlRestClientAsync(ServiceClientBaseUri);
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
				return new JsonRestClientAsync(ServiceClientBaseUri);
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
				return new JsvRestClientAsync(ServiceClientBaseUri);
			}
		}

#if !MONOTOUCH

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
		}

#endif

	}
}