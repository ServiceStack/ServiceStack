using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.ServiceHost.Tests
{
	[TestFixture]
	public class ServiceStackHandlerUrlTests
	{
		public static string ResolvePath(string mode, string path)
		{
			return WebHost.Endpoints.Extensions.HttpRequestExtensions.
				GetPathInfo(path, mode, path.Split('/').First(x => x != ""));
		}

		public class MockUrlHttpRequest : IHttpRequest
		{
			public MockUrlHttpRequest() { }

			public MockUrlHttpRequest(string mode, string path, string rawUrl)
			{
				this.PathInfo = ResolvePath(mode, path);
				this.RawUrl = rawUrl;
				AbsoluteUri = "http://localhost" + rawUrl;
			}

			public object OriginalRequest
			{
				get { throw new NotImplementedException(); }
			}

			public T TryResolve<T>()
			{
				throw new NotImplementedException();
			}

			public string OperationName { get; private set; }
			public string ContentType { get; private set; }
			public string HttpMethod { get; private set; }
			public string UserAgent { get; set; }
            public bool IsLocal { get; set; }

			public IDictionary<string, Cookie> Cookies { get; private set; }
			public string ResponseContentType { get; set; }
			public Dictionary<string, object> Items { get; private set; }
			public NameValueCollection Headers { get; private set; }
			public NameValueCollection QueryString { get; private set; }
			public NameValueCollection FormData { get; private set; }
		    public bool UseBufferedStream { get; set; }

		    public string GetRawBody()
			{
				throw new NotImplementedException();
			}

			public string RawUrl { get; private set; }
			public string AbsoluteUri { get; set; }
			public string UserHostAddress { get; private set; }

            public string RemoteIp { get; set; }
            public string XForwardedFor { get; set; }
            public string XRealIp { get; set; }

		    public bool IsSecureConnection { get; private set; }
			public string[] AcceptTypes { get; private set; }
			public string PathInfo { get; private set; }
			public Stream InputStream { get; private set; }
			public long ContentLength { get; private set; }
			public IFile[] Files { get; private set; }

			public string ApplicationFilePath { get; private set; }
		}

		readonly List<MockUrlHttpRequest> allResults = new List<MockUrlHttpRequest> {
			new MockUrlHttpRequest(null, "/handler.all35/json/metadata", "/handler.all35/json/metadata?op=Hello"),
			new MockUrlHttpRequest(null, "/handler.all35/json/metadata/", "/handler.all35/json/metadata/?op=Hello"),
		};

		readonly List<MockUrlHttpRequest> apiResults = new List<MockUrlHttpRequest> {
			new MockUrlHttpRequest(null, "/location.api.wildcard35/api/json/metadata", "/location.api.wildcard35/api/json/metadata?op=Hello"),
			new MockUrlHttpRequest(null, "/location.api.wildcard35/api/json/metadata/", "/location.api.wildcard35/api/json/metadata/?op=Hello"),
		};

		readonly List<MockUrlHttpRequest> serviceStacksResults = new List<MockUrlHttpRequest> {
			new MockUrlHttpRequest(null, "/location.servicestack.wildcard35/servicestack/json/metadata", "/location.servicestack.wildcard35/servicestack/json/metadata?op=Hello"),
			new MockUrlHttpRequest(null, "/location.servicestack.wildcard35/servicestack/json/metadata/", "/location.servicestack.wildcard35/servicestack/json/metadata/?op=Hello"),
		};

		[Test]
		public void Does_return_expected_absolute_and_path_urls()
		{
			var absolutePaths = allResults.ConvertAll(x => x.GetAbsolutePath());
			var pathUrls = allResults.ConvertAll(x => x.GetPathUrl());
			Assert.That(absolutePaths.All(x => x == "/handler.all35/json/metadata"));
			Assert.That(pathUrls.All(x => x == "http://localhost/handler.all35/json/metadata"));

			absolutePaths = apiResults.ConvertAll(x => x.GetAbsolutePath());
			pathUrls = apiResults.ConvertAll(x => x.GetPathUrl());
			Assert.That(absolutePaths.All(x => x == "/location.api.wildcard35/api/json/metadata"));
			Assert.That(pathUrls.All(x => x == "http://localhost/location.api.wildcard35/api/json/metadata"));

			absolutePaths = serviceStacksResults.ConvertAll(x => x.GetAbsolutePath());
			pathUrls = serviceStacksResults.ConvertAll(x => x.GetPathUrl());
			Assert.That(absolutePaths.All(x => x == "/location.servicestack.wildcard35/servicestack/json/metadata"));
			Assert.That(pathUrls.All(x => x == "http://localhost/location.servicestack.wildcard35/servicestack/json/metadata"));
		}

		[Test]
		public void Does_return_expected_parent_absolute_and_path_urls()
		{
			var absolutePaths = allResults.ConvertAll(x => x.GetParentAbsolutePath());
			var pathUrls = allResults.ConvertAll(x => x.GetParentPathUrl());

			Assert.That(absolutePaths.All(x => x == "/handler.all35/json"));
			Assert.That(pathUrls.All(x => x == "http://localhost/handler.all35/json"));

			absolutePaths = apiResults.ConvertAll(x => x.GetParentAbsolutePath());
			pathUrls = apiResults.ConvertAll(x => x.GetParentPathUrl());
			Assert.That(absolutePaths.All(x => x == "/location.api.wildcard35/api/json"));
			Assert.That(pathUrls.All(x => x == "http://localhost/location.api.wildcard35/api/json"));

			absolutePaths = serviceStacksResults.ConvertAll(x => x.GetParentAbsolutePath());
			pathUrls = serviceStacksResults.ConvertAll(x => x.GetParentPathUrl());
			Assert.That(absolutePaths.All(x => x == "/location.servicestack.wildcard35/servicestack/json"));
			Assert.That(pathUrls.All(x => x == "http://localhost/location.servicestack.wildcard35/servicestack/json"));
		}

		[Test]
		public void Can_Get_UrlHostName()
		{
			var urls = new List<string> { "http://localhost/a", "https://localhost/a", 
				"http://localhost:81", "http://localhost:81/", "http://localhost" };

			var httpReqs = urls.ConvertAll(x => new MockUrlHttpRequest { AbsoluteUri = x });
			var hostNames = httpReqs.ConvertAll(x => x.GetUrlHostName());

			Console.WriteLine(hostNames.Dump());

			Assert.That(hostNames.All(x => x == "localhost"));
		}

	}
}