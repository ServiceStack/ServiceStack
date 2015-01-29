using System;
using System.Collections.Specialized;
using System.IO;
using System.Web;
using Funq;
using NUnit.Framework;
using ServiceStack.Caching;
using ServiceStack.Host.AspNet;
using ServiceStack.Testing;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    [TestFixture]
    public class SessionTests
    {
        [Test]
        public void Adhoc()
        {
            var appliesTo = ApplyTo.Post | ApplyTo.Put;
            Console.WriteLine(appliesTo.ToString());
            Console.WriteLine(appliesTo.ToDescription());
            Console.WriteLine(string.Join(", ", appliesTo.ToList().ToArray()));
        }

        [Test]
        public void Can_mock_Session_when_accessed_via_HttpRequest_Context()
        {
            using (new BasicAppHost().Init())
            {
                HttpContext.Current = new HttpContext(
                    new HttpRequest(null, "http://example.com", null),
                    new HttpResponse(new StringWriter()));

                HttpContext.Current.Items[SessionFeature.RequestItemsSessionKey] =
                    new AuthUserSession
                    {
                        Id = "mock-session-id",
                    };

                var httpReq = HttpContext.Current.ToRequest();
                var session = httpReq.GetSession();

                Assert.That(session, Is.Not.Null);
                Assert.That(session.Id, Is.EqualTo("mock-session-id"));
            }
        }

    }
}