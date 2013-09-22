#if !SILVERLIGHT && !MONOTOUCH && !XBOX && !ANDROIDINDIE
using System;
using System.ServiceModel.Channels;
using ServiceStack.Clients;
using System.Xml;

namespace ServiceStack.Clients
{
    public interface IWcfServiceClient : IServiceClient
    {
        string Uri { get; set; }
        void SetProxy(Uri proxyAddress);
        Message Send(object request);
        Message Send(object request, string action);
        Message Send(XmlReader reader, string action);
        Message Send(Message message);
        void SendOneWay(object request, string action);
        void SendOneWay(XmlReader reader, string action);
        void SendOneWay(Message message);
    }
}
#endif