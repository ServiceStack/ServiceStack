using System;
using System.Runtime.Serialization;
using System.Text;
using Funq;
using NUnit.Framework;
using ServiceStack.Service;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.ServiceInterface.Testing;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Support;
using ServiceStack.WebHost.Endpoints.Tests.Support;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	[DataContract]
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

	public abstract class RequestFiltersTests
	{
		private const string ListeningOn = "http://localhost:82/";
		private const string ServiceClientBaseUri = "http://localhost:82/";

		public class RequestFiltersAppHostHttpListener
			: AppHostHttpListenerBase
		{
			private const string AllowedUser = "user";
			private const string AllowedPass = "p@55word";

			public RequestFiltersAppHostHttpListener()
				: base("Request Filters Tests", typeof(GetFactorialService).Assembly) { }

			public override void Configure(Container container)
			{
				this.RequestFilters.Add((req, res, dto) =>
				{
                	var userPass = req.GetBasicAuthUserAndPassword();
					if (userPass != null)
					{
						var userName = userPass.Value.Key;
						if (userName == AllowedUser && userPass.Value.Value == AllowedPass)
						{
							res.SetPermanentCookie("ss-session", userName + "/" + Guid.NewGuid().ToString("N"));
						}
						else
						{
							res.ReturnAuthRequired();
						}
					}
				});
				this.RequestFilters.Add((req, res, dto) =>
				{
					if (dto is Secure)
					{
						var sessionId = req.GetCookieValue("ss-session");
						if (sessionId == null || sessionId.SplitFirst('/')[0] != AllowedUser)
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
		}

		protected abstract IServiceClient CreateNewServiceClient();

		[Test]
		public void Get_401_When_accessing_Secure_without_Authorization()
		{
			var client = CreateNewServiceClient();

			try
			{
				var response = client.Send<SecureResponse>(new Secure());
				Console.WriteLine(response.Dump());
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				return;
			}
			Assert.Fail("Should throw");
		}


		[TestFixture]
		public class UnitTests : RequestFiltersTests
		{
			protected override IServiceClient CreateNewServiceClient()
			{
				EndpointHandlerBase.ServiceManager = new ServiceManager(true, typeof(SecureService).Assembly);
				return new DirectServiceClient(EndpointHandlerBase.ServiceManager);
			}
		}

		[TestFixture]
		public class XmlIntegrationTests : RequestFiltersTests
		{
			protected override IServiceClient CreateNewServiceClient()
			{
				return new XmlServiceClient(ServiceClientBaseUri);
			}
		}

		[TestFixture]
		public class JsonIntegrationTests : RequestFiltersTests
		{
			protected override IServiceClient CreateNewServiceClient()
			{
				return new JsonServiceClient(ServiceClientBaseUri);
			}
		}

		[TestFixture]
		public class JsvIntegrationTests : RequestFiltersTests
		{
			protected override IServiceClient CreateNewServiceClient()
			{
				return new JsvServiceClient(ServiceClientBaseUri);
			}
		}

		[TestFixture]
		public class Soap11IntegrationTests : RequestFiltersTests
		{
			protected override IServiceClient CreateNewServiceClient()
			{
				return new Soap11ServiceClient(ServiceClientBaseUri);
			}
		}

		[TestFixture]
		public class Soap12IntegrationTests : RequestFiltersTests
		{
			protected override IServiceClient CreateNewServiceClient()
			{
				return new Soap12ServiceClient(ServiceClientBaseUri);
			}
		}
	}
}