using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Testing;
using ServiceStack.Text;

namespace ServiceStack.ServiceHost.Tests
{
    public class RequestPath
    {
        public RequestPath(string path, string host, string pathInfo, string rawUrl)
        {
            Path = path;
            Host = host;
            PathInfo = pathInfo;
            RawUrl = rawUrl;
            AbsoluteUri = "http://localhost" + rawUrl;
        }

        public string Path { get; set; }
        public string Host { get; set; }
        public string PathInfo { get; set; }
        public string RawUrl { get; set; }
        public string AbsoluteUri { get; set; }
    }

    [TestFixture]
    public class ServiceStackHandlerPathTests
    {
        public string ResolvePath(string mode, string path)
        {
            return HttpRequestExtensions.
                GetPathInfo(path, mode, path.Split('/').First(x => x != ""));
        }

        [Test]
        public void Can_resolve_root_path()
        {
            var results = new List<string> {
                ResolvePath(null, "/handler.all35"),
                ResolvePath(null, "/handler.all35/"),
                ResolvePath("api", "/location.api.wildcard35/api"),
                ResolvePath("api", "/location.api.wildcard35/api/"),
                ResolvePath("servicestack", "/location.servicestack.wildcard35/servicestack"),
                ResolvePath("servicestack", "/location.servicestack.wildcard35/servicestack/"),
            };

            Console.WriteLine(results.Dump());

            Assert.That(results.All(x => x == "/"));
        }

        [Test]
        public void Can_resolve_metadata_paths()
        {
            var results = new List<string> {
                ResolvePath(null, "/handler.all35/metadata"),
                ResolvePath(null, "/handler.all35/metadata/"),
                ResolvePath("api", "/location.api.wildcard35/api/metadata"),
                ResolvePath("api", "/location.api.wildcard35/api/metadata/"),
                ResolvePath("servicestack", "/location.servicestack.wildcard35/servicestack/metadata"),
                ResolvePath("servicestack", "/location.servicestack.wildcard35/servicestack/metadata/"),
            };

            Console.WriteLine(results.Dump());

            Assert.That(results.All(x => x == "/metadata"));
        }

        [Test]
        public void Can_resolve_metadata_json_paths()
        {
            var results = new List<string> {
                ResolvePath(null, "/handler.all35/json/metadata"),
                ResolvePath(null, "/handler.all35/json/metadata/"),
                ResolvePath("api", "/location.api.wildcard35/api/json/metadata"),
                ResolvePath("api", "/location.api.wildcard35/api/json/metadata/"),
                ResolvePath("servicestack", "/location.api.wildcard35/servicestack/json/metadata"),
                ResolvePath("servicestack", "/location.api.wildcard35/servicestack/json/metadata/"),
            };

            Console.WriteLine(results.Dump());

            Assert.That(results.All(x => x == "/json/metadata"));
        }

        [Test]
        public void Can_resolve_paths_with_multipart_root()
        {
            var results = new List<string> {
                HttpRequestExtensions.GetPathInfo("/api/foo/metadata", "api/foo", "api"),
                HttpRequestExtensions.GetPathInfo("/api/foo/1.0/wildcard/metadata", "api/foo/1.0/wildcard", "api"),
                HttpRequestExtensions.GetPathInfo("/location.api.wildcard35/api/foo/metadata", "api/foo", "api"),
                HttpRequestExtensions.GetPathInfo("/this/is/very/nested/metadata", "this/is/very/nested", "api"),
            };

            Console.WriteLine(results.Dump());

            Assert.That(results.All(x => x == "/metadata"));
        }

        [Test]
        public void GetPhysicalPath_Honours_WebHostPhysicalPath()
        {
            using (var appHost = new BasicAppHost
            {
                ConfigFilter = c =>
                {
                    c.WebHostPhysicalPath = "c:/Windows/Temp";
                }
            }.Init())
            {
                var mock = new HttpRequestMock();

                string originalPath = appHost.Config.WebHostPhysicalPath;
                string path = mock.GetPhysicalPath();

                var normalizePaths = (Func<string, string>)(p => p == null ? null : p.Replace('\\', '/'));

                Assert.That(normalizePaths(path), Is.EqualTo(normalizePaths("{0}/{1}".Fmt(originalPath, mock.PathInfo))));
            }
        }
    }

}
