﻿using NUnit.Framework;
using ServiceStack.Formats;
using ServiceStack.Host;
using ServiceStack.ServiceHost.Tests.Formats;
using ServiceStack.Testing;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.ServiceHost.Tests
{
    [TestFixture]
    public class RequestContextExtensionsTest
    {
        [Test]
        public void Can_optimize_html_result_with_ToOptimizedResult()
        {
            CanOptimizeResult("text/html", new HtmlFormat());
        }

        [Test]
        public void Can_optimize_csv_result_with_ToOptimizedResult()
        {
            CanOptimizeResult("text/csv", new CsvFormat());
        }

        [Test]
        public void Can_optimize_json_result_with_ToOptimizedResult()
        {
            CanOptimizeResult(MimeTypes.Json, null);
        }

        [Test]
        public void Can_optimize_xml_result_with_ToOptimizedResult()
        {
            CanOptimizeResult(MimeTypes.Xml, null);
        }

        [Test]
        public void Can_optimize_jsv_result_with_ToOptimizedResult()
        {
            CanOptimizeResult(MimeTypes.Jsv, null);
        }

        private static void CanOptimizeResult(string contentType, IPlugin pluginFormat)
        {
            using (var appHost = new BasicAppHost().Init())
            {
                var dto = new TestDto { Name = "test" };

                var httpReq = new MockHttpRequest();
                httpReq.Headers.Add(HttpHeaders.AcceptEncoding, "gzip,deflate,sdch");
                httpReq.ResponseContentType = contentType;

                if (pluginFormat != null) pluginFormat.Register(appHost);

                object result = httpReq.ToOptimizedResult(dto);
                Assert.IsNotNull(result);
                Assert.IsTrue(result is CompressedResult);
            }
        }

        public class TestDto
        {
            public string Name { get; set; }
        }
    }
}