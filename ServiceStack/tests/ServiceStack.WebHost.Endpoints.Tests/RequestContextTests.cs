using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Funq;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.Text;
using ServiceStack.Web;
using ServiceStack.WebHost.Endpoints.Tests.Support.Services;
#pragma warning disable CS0618

namespace ServiceStack.WebHost.Endpoints.Tests;

[TestFixture]
public class RequestContextTests
{
	public class HeadersAppHostHttpListener()
		: AppHostHttpListenerBase("Request Filters Tests", typeof(HeadersService).Assembly)
	{
		public override void Configure(Container container)
		{
			HostContext.Config.GlobalResponseHeaders.Clear();

			//Signal advanced web browsers what HTTP Methods you accept
			base.SetConfig(new HostConfig
			{
				GlobalResponseHeaders =
				{
					{ "Access-Control-Allow-Origin", "*" },
					{ "Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS" },
				},
			});

			this.GlobalRequestFilters.Add((req, res, dto) =>
			{
				if (dto is RequestFilter requestFilter)
				{
					res.StatusCode = requestFilter.StatusCode;
					if (!requestFilter.HeaderName.IsNullOrEmpty())
					{
						res.AddHeader(requestFilter.HeaderName, requestFilter.HeaderValue);
					}
				}
			});
		}
	}

	HeadersAppHostHttpListener appHost;

	[OneTimeSetUp]
	public void OnTestFixtureSetUp()
	{
		appHost = new HeadersAppHostHttpListener();
		appHost.Init();
		appHost.Start(Config.ListeningOn);
	}

	[OneTimeTearDown]
	public void OnTestFixtureTearDown()
	{
		appHost.Dispose();
	}

	public static Dictionary<string, string> GetResponseHeaders(String url)
	{
		try
		{
#pragma warning disable CS0618, SYSLIB0014
			var webReq = WebRequest.CreateHttp(url);
#pragma warning restore CS0618, SYSLIB0014

			var webResponse = webReq.GetResponse();

			var map = new Dictionary<string, string>();
			for (var i = 0; i < webResponse.Headers.Count; i++)
			{
				var header = webResponse.Headers.AllKeys[i];
				map[header] = webResponse.Headers[header];
			}

			return map;
		}
		catch (WebException e)
		{
			Console.WriteLine(e);
			throw;
		}
	}

	[Test]
	public void Can_resolve_CustomHeader()
	{
#pragma warning disable CS0618, SYSLIB0014
		var webRequest = WebRequest.CreateHttp(
			Config.ListeningOn + "json/reply/Headers?Name=X-CustomHeader");
#pragma warning restore CS0618, SYSLIB0014
		webRequest.Headers["X-CustomHeader"] = "CustomValue";

		var response = JsonSerializer.DeserializeFromStream<HeadersResponse>(
			webRequest.GetResponse().GetResponseStream());

		Assert.That(response.Value, Is.EqualTo("CustomValue"));
	}

	[Test]
	public void Does_Send_Global_Headers()
	{
		var headers = GetResponseHeaders(Config.ListeningOn + "json/reply/Headers");
		Assert.That(headers["Access-Control-Allow-Origin"], Is.EqualTo("*"));
		Assert.That(headers["Access-Control-Allow-Methods"], Is.EqualTo("GET, POST, PUT, DELETE, OPTIONS"));
	}

	[Test]
	public void Does_return_bare_401_StatusCode()
	{
		try
		{
#pragma warning disable CS0618, SYSLIB0014
			var webRequest = WebRequest.CreateHttp(
				Config.ListeningOn + "json/reply/RequestFilter?StatusCode=401");
#pragma warning restore CS0618, SYSLIB0014

			webRequest.GetResponse();

			Assert.Fail("Should throw 401 WebException");
		}
		catch (WebException ex)
		{
			var httpResponse = (HttpWebResponse)ex.Response;
			Assert.That(httpResponse!.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
		}
	}

	[Test]
	public void Does_return_bare_401_with_AuthRequired_header()
	{
		try
		{
#pragma warning disable CS0618, SYSLIB0014
			var webRequest = WebRequest.CreateHttp(Config.ListeningOn 
			                                       + "json/reply/RequestFilter?StatusCode=401"
			                                       + "&HeaderName=" + HttpHeaders.WwwAuthenticate
			                                       + "&HeaderValue=" + "Basic realm=\"Auth Required\"".UrlEncode());
#pragma warning restore CS0618, SYSLIB0014

			webRequest.GetResponse();

			Assert.Fail("Should throw 401 WebException");
		}
		catch (WebException ex)
		{
			var httpResponse = (HttpWebResponse)ex.Response;
			Assert.That(httpResponse!.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

			Assert.That(ex.Response!.Headers[HttpHeaders.WwwAuthenticate],
				Is.EqualTo("Basic realm=\"Auth Required\""));
		}
	}


}