using System;
using NUnit.Framework;
using ServiceStack.Host;
using ServiceStack.Testing;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests.ServiceClient.Web
{
    [TestFixture]
    public class UrlExtensionsTests
    {
        [Test]
        public void FormatVariable_DateTimeOffsetValue_ValueIsUrlEncoded()
        {
            var dateTimeOffset = DateTimeOffset.Now;
            var formattedVariable = RestRoute.FormatVariable(dateTimeOffset);
            var jsv = dateTimeOffset.ToJsv();
            Assert.AreEqual(Uri.EscapeDataString(jsv), formattedVariable);
        }

        [Test]
        public void FormatQueryParameterValue_DateTimeOffsetValue_ValueIsUrlEncoded()
        {
            var dateTimeOffset = DateTimeOffset.Now;
            var formattedVariable = RestRoute.FormatQueryParameterValue(dateTimeOffset);
            var jsv = dateTimeOffset.ToJsv();
            Assert.AreEqual(Uri.EscapeDataString(jsv), formattedVariable);
        }

        [Test]
        public void Can_get_operation_name()
        {
            Assert.That(typeof(Root).GetOperationName(), Is.EqualTo("Root"));
            Assert.That(typeof(Root.Nested).GetOperationName(), Is.EqualTo("Root.Nested"));
        }

        [Test]
        public void Can_use_nested_classes_as_Request_DTOs()
        {
            using (var appHost = new BasicAppHost(typeof(NestedService).Assembly){}.Init())
            {
                var root = (Root)appHost.ExecuteService(new Root { Id = 1 });
                Assert.That(root.Id, Is.EqualTo(1));

                var nested = (Root.Nested)appHost.ExecuteService(new Root.Nested { Id = 2 });
                Assert.That(nested.Id, Is.EqualTo(2));
            }
        }
    }

    public class Root
    {
        public int Id { get; set; }

        public class Nested
        {
            public int Id { get; set; }
        }
    }

    public class NestedService : Service
    {
        public object Any(Root request)
        {
            return request;
        }

        public object Any(Root.Nested request)
        {
            return request;
        }
    }
}
