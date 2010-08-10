using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ServiceStack.ServiceClient.Web;

namespace ServiceStack.WebHost.Endpoints.Tests.IntegrationTests
{
	[TestFixture]
	public class IntegrationTestBase
	{
		private const string BaseUrl = "http://127.0.0.1:8080/";
		private AppHost appHost;

		[TestFixtureSetUp]
		public virtual void TestFixtureSetUp()
		{
			appHost = new AppHost();
			appHost.Init();
			try
			{
				appHost.Start(BaseUrl);
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error trying to run ConsoleHost: " + ex.Message);
			}
		}

		public void SendToEachEndpoint<TRes>(object request, Action<TRes> validate)
		{
			SendToEachEndpoint(request, null, validate);
		}

		/// <summary>
		/// Run the request against each Endpoint
		/// </summary>
		/// <typeparam name="TRes"></typeparam>
		/// <param name="request"></param>
		/// <param name="validate"></param>
		/// <param name="httpMethod"></param>
		public void SendToEachEndpoint<TRes>(object request, string httpMethod, Action<TRes> validate)
		{
			using (var xmlClient = new XmlServiceClient(BaseUrl))
			using (var jsonClient = new JsonServiceClient(BaseUrl))
			using (var jsvClient = new JsvServiceClient(BaseUrl))
			{
				xmlClient.HttpMethod = httpMethod;
				jsonClient.HttpMethod = httpMethod;
				jsvClient.HttpMethod = httpMethod;

				var xmlResponse = xmlClient.Send<TRes>(request);
				if (validate != null) validate(xmlResponse);

				var jsonResponse = jsonClient.Send<TRes>(request);
				if (validate != null) validate(jsonResponse);

				var jsvResponse = jsvClient.Send<TRes>(request);
				if (validate != null) validate(jsvResponse);
			}
		}
	}
}