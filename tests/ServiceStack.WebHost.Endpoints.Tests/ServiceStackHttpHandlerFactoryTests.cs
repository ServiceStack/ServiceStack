using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Host;
using ServiceStack.Host.Handlers;
using ServiceStack.Metadata;
using ServiceStack.Testing;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [TestFixture]
    public class ServiceStackHttpHandlerFactoryTests
    {
        ServiceStackHost appHost;

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new BasicAppHost(GetType().Assembly)
            {
                ConfigureAppHost = host =>
                {
                    host.Plugins.Add(new PredefinedRoutesFeature());
#if !NETCORE
                    host.Plugins.Add(new SoapFormat());
#endif
                    host.CatchAllHandlers.Add(new PredefinedRoutesFeature().ProcessRequest);
                    host.CatchAllHandlers.Add(new MetadataFeature().ProcessRequest);
                }
            }.Init();
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        readonly Dictionary<string, Type> pathInfoMap = new Dictionary<string, Type>
		{
            {"Metadata", typeof(IndexMetadataHandler)},
#if !NETCORE            
            {"Soap11", typeof(Soap11MessageReplyHttpHandler)},
            {"Soap12", typeof(Soap12MessageReplyHttpHandler)},
#endif

            {"Json/Reply", typeof(JsonReplyHandler)},
            {"Json/OneWay", typeof(JsonOneWayHandler)},
            {"Json/Metadata", typeof(JsonMetadataHandler)},

            {"Xml/Reply", typeof(XmlReplyHandler)},
            {"Xml/OneWay", typeof(XmlOneWayHandler)},
            {"Xml/Metadata", typeof(XmlMetadataHandler)},

            {"Jsv/Reply", typeof(JsvReplyHandler)},
            {"Jsv/OneWay", typeof(JsvOneWayHandler)},
            {"Jsv/Metadata", typeof(JsvMetadataHandler)},

#if !NETCORE
			{"Soap11/Wsdl", typeof(Soap11WsdlMetadataHandler)},
			{"Soap11/Metadata", typeof(Soap11MetadataHandler)},

			{"Soap12/Wsdl", typeof(Soap12WsdlMetadataHandler)},
			{"Soap12/Metadata", typeof(Soap12MetadataHandler)},
#endif
		};

        [Test]
        public void Resolves_the_right_handler_for_expexted_paths()
        {
            foreach (var item in pathInfoMap)
            {
                var expectedType = item.Value;
                var httpReq = new BasicHttpRequest
                {
                    PathInfo = item.Key,
                };
                var handler = HttpHandlerFactory.GetHandlerForPathInfo(httpReq, null);
                Assert.That(handler.GetType(), Is.EqualTo(expectedType));
            }
        }

        [Test]
        public void Resolves_the_right_handler_for_case_insensitive_expected_paths()
        {
            foreach (var item in pathInfoMap)
            {
                var expectedType = item.Value;
                var lowerPathInfo = item.Key.ToLower();
                lowerPathInfo.Print();
                var httpReq = new BasicHttpRequest
                {
                    PathInfo = lowerPathInfo,
                };
                var handler = HttpHandlerFactory.GetHandlerForPathInfo(httpReq, null);
                Assert.That(handler?.GetType(), Is.EqualTo(expectedType));
            }
        }
    }
}