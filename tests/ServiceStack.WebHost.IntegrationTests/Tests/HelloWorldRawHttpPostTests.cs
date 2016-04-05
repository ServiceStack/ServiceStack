using System;
using System.IO;
using System.Net;
using NUnit.Framework;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    public class HelloWorldRawHttpPostTests
    {
        [Test]
        public void Post_JSON_to_HelloWorld()
        {
            var httpReq = (HttpWebRequest)WebRequest.Create(Config.ServiceStackBaseUri + "/hello");
            httpReq.Method = "POST";
            httpReq.ContentType = httpReq.Accept = "application/json";

            using (var stream = httpReq.GetRequestStream())
            using (var sw = new StreamWriter(stream))
            {
                sw.Write("{\"Name\":\"World!\"}");
            }

            using (var response = httpReq.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                Assert.That(reader.ReadToEnd(), Is.EqualTo("{\"result\":\"Hello, World!\"}"));
            }
        }

        [Test]
        public void Post_XML_to_HelloWorld()
        {
            var httpReq = (HttpWebRequest)WebRequest.Create(Config.ServiceStackBaseUri + "/hello");
            httpReq.Method = "POST";
            httpReq.ContentType = httpReq.Accept = "application/xml";

            using (var stream = httpReq.GetRequestStream())
            using (var sw = new StreamWriter(stream))
            {
                sw.Write("<Hello xmlns=\"http://schemas.servicestack.net/types\"><Name>World!</Name></Hello>");
            }

            using (var response = httpReq.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                Assert.That(reader.ReadToEnd(), Is.EqualTo("<?xml version=\"1.0\" encoding=\"utf-8\"?><HelloResponse xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://schemas.servicestack.net/types\"><Result>Hello, World!</Result></HelloResponse>"));
            }
        }
    }

}