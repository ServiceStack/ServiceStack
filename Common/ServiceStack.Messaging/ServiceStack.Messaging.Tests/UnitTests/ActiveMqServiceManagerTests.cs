using System;
using System.Collections.Generic;
using System.Xml;
using ServiceStack.Common.Services.Serialization;
using ServiceStack.Messaging.ActiveMq;
using ServiceStack.Messaging.Tests.Objects.Exceptions;
using ServiceStack.Messaging.Tests.Objects.Serializable;
using ServiceStack.Messaging.Tests.Services.Basic;
using ServiceStack.Messaging.Tests.Services.Messaging;
using NUnit.Framework;

namespace ServiceStack.Messaging.Tests.UnitTests
{
    [TestFixture]
    public class ActiveMqServiceManagerTests : UnitTestCaseBase
    {
        private IServiceHost CreateServiceHost(IConnection connection, string destinationUri, Type serviceType)
        {
            ActiveMqServiceHostConfigQueue hostConfig = new ActiveMqServiceHostConfigQueue();
            hostConfig.Uri = destinationUri;
            hostConfig.FailoverSettings.BrokerUris.AddRange(FailoverUris);
            hostConfig.ServiceType = serviceType;
            IDestination destination = new Destination(DestinationType.Queue, destinationUri);
            IGatewayListener listener = connection.CreateListener(destination);
            return MockFactory.CreateServiceHost(listener, hostConfig);
        }

        protected string ReverseDestinationUri
        {
            get { return DestinationUri + ".Reverse"; }
        }

        protected string GreetDestinationUri
        {
            get { return DestinationUri + ".Greet"; }
        }

        protected IDestination ReverseDestination
        {
            get { return new Destination(DestinationType.Queue, ReverseDestinationUri); }
        }

        protected IDestination GreetDestination
        {
            get { return new Destination(DestinationType.Queue, GreetDestinationUri); }
        }

        [Test]
        public void ActiveMqServiceManager_SingleTextServiceTest()
        {
            using (IConnection connection = CreateNewConnection())
            {
                int noOfRequests = new Random().Next(2, 10);
                List<IServiceHost> serviceHosts = new List<IServiceHost>();
                serviceHosts.Add(CreateServiceHost(connection, ReverseDestinationUri, typeof(ReverseTextService)));
                List<string> responses = new List<string>();

                using (ActiveMqServiceManager manager = new ActiveMqServiceManager(serviceHosts))
                {
                    manager.Start();

                    using (IReplyClient client = connection.CreateReplyClient(ReverseDestination))
                    {
                        for (int i = 0; i < noOfRequests; i++)
                        {
                            ITextMessage response = client.Send(TextMessage, MockWaitTimeOut);
                            Assert.AreEqual(SimpleService.Reverse(TEXT_MESSAGE), response.Text);
                            responses.Add(response.Text);
                        }
                    }
                }
                Assert.AreEqual(noOfRequests, responses.Count);
            }
            AssertAllResourcesDisposed(MockFactory);
        }

        [Test]
        public void ActiveMqServiceManager_MultipleTextServicesTest()
        {
            using (IConnection connection = CreateNewConnection())
            {
                int noOfRequests = new Random().Next(2, 10);
                List<IServiceHost> serviceHosts = new List<IServiceHost>();
                serviceHosts.Add(CreateServiceHost(connection, ReverseDestinationUri, typeof (ReverseTextService)));
                serviceHosts.Add(CreateServiceHost(connection, GreetDestinationUri, typeof (GreetTextService)));
                List<string> reverseResponses = new List<string>();
                List<string> greetResponses = new List<string>();

                using (ActiveMqServiceManager manager = new ActiveMqServiceManager(serviceHosts))
                {
                    manager.Start();

                    using (IReplyClient client = connection.CreateReplyClient(ReverseDestination))
                    {
                        for (int i = 0; i < noOfRequests; i++)
                        {
                            ITextMessage response = client.Send(TextMessage, MockWaitTimeOut);
                            Assert.AreEqual(SimpleService.Reverse(TEXT_MESSAGE), response.Text);
                            reverseResponses.Add(response.Text);
                        }
                    }

                    using (IReplyClient client = connection.CreateReplyClient(GreetDestination))
                    {
                        for (int i = 0; i < noOfRequests; i++)
                        {
                            ITextMessage response = client.Send(TextMessage, MockWaitTimeOut);
                            Assert.AreEqual(SimpleService.Greet(TEXT_MESSAGE), response.Text);
                            greetResponses.Add(response.Text);
                        }
                    }
                }

                Assert.AreEqual(noOfRequests, reverseResponses.Count);
                Assert.AreEqual(noOfRequests, greetResponses.Count);
            }
        }

        [Test]
        public void ActiveMqServiceManager_SingleXmlSerializableServiceTest()
        {
            using (IConnection connection = CreateNewConnection())
            {
                int noOfRequests = new Random().Next(2, 10);
                List<IServiceHost> serviceHosts = new List<IServiceHost>();
                serviceHosts.Add(
                    CreateServiceHost(connection, ReverseDestinationUri, typeof(ReverseXmlSerializableService)));
                List<string> responses = new List<string>();

                using (ActiveMqServiceManager manager = new ActiveMqServiceManager(serviceHosts))
                {
                    manager.Start();

                    using (IReplyClient client = connection.CreateReplyClient(ReverseDestination))
                    {
                        for (int i = 0; i < noOfRequests; i++)
                        {
                            var request = new XmlSerializableObject();
                            request.Value = TEXT_MESSAGE;
                            var requestXml = new XmlSerializableSerializer().Parse(request);
                            var responseXml = client.Send(new TextMessage(requestXml), MockWaitTimeOut);

                            var response = new XmlSerializableDeserializer().Parse<XmlSerializableObject>(responseXml.Text);
                            Assert.AreEqual(SimpleService.Reverse(TEXT_MESSAGE), response.Value);
                            responses.Add(responseXml.Text);
                        }
                    }
                }

                Assert.AreEqual(noOfRequests, responses.Count);
            }
        }

        [Test]
        public void ActiveMqServiceManager_ExceptionServiceTest()
        {
            using (IConnection connection = CreateNewConnection())
            {
                List<IServiceHost> serviceHosts = new List<IServiceHost>();
                serviceHosts.Add(CreateServiceHost(connection, GreetDestinationUri, typeof(ExceptionService)));

                using (ActiveMqServiceManager manager = new ActiveMqServiceManager(serviceHosts))
                {
                    manager.Start();

                    using (IReplyClient client = connection.CreateReplyClient(GreetDestination))
                    {
                        XmlSerializableObject request = new XmlSerializableObject();
                        request.Value = TEXT_MESSAGE;
                        string requestXml = new XmlSerializableSerializer().Parse(request);
                        ITextMessage responseXml = client.Send(new TextMessage(requestXml), MockWaitTimeOut);
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(responseXml.Text);
                        XmlNamespaceManager nsm = new XmlNamespaceManager(doc.NameTable);
                        nsm.AddNamespace("s", "http://www.w3.org/2003/05/soap-envelope");
                        MessagingException expectedException = new MessagingException("ExceptionService.MessagingException");
                        Assert.IsNotNull(doc.SelectSingleNode("/s:Envelope/s:Body/s:Fault", nsm), "Response Exception is not a SOAP Fault");
                        Assert.AreEqual(expectedException.GetType().FullName,
                            doc.SelectSingleNode("/s:Envelope/s:Body/s:Fault/s:Code/s:Value", nsm).InnerText);
                        Assert.AreEqual(expectedException.Message,
                            doc.SelectSingleNode("/s:Envelope/s:Body/s:Fault/s:Reason/s:Text", nsm).InnerText);
                        Assert.AreEqual(expectedException.ToXml(),
                            doc.SelectSingleNode("/s:Envelope/s:Body/s:Fault/s:Detail/s:Exception/Body", nsm).InnerText);
                    }
                }
            }
        }

    }
}
