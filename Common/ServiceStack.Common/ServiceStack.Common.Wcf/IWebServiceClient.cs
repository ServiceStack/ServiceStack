using System;
using System.ServiceModel.Channels;
using System.Xml;

namespace ServiceStack.Common.Wcf
{
    public interface IWebServiceClient : IDisposable
    {
        string Uri { get; set; }
        Binding Binding { get; set; }
        bool UseBasicHttpBinding { set; }
        void SetProxy(Uri proxyAddress);
        Message Send(object request);
        Message Send(object request, string action);
        Message Send(XmlReader reader, string action);
        Message Send(Message message);
        void SendOneWay(object request);
        void SendOneWay(object request, string action);
        void SendOneWay(XmlReader reader, string action);
        void SendOneWay(Message message);
    }
}