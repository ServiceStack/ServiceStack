#if !SILVERLIGHT && !MONOTOUCH && !XBOX && !ANDROIDINDIE
using System;
using System.ServiceModel.Channels;
using ServiceStack.Service;
using System.Xml;

namespace ServiceStack.ServiceClient.Web
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