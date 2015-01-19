using ServiceStack.Text;

namespace ServiceStack.Host.Handlers
{
    public class XmlOneWayHandler : GenericHandler
    {
        public XmlOneWayHandler()
            : base(MimeTypes.Xml, RequestAttributes.OneWay | RequestAttributes.Xml, Feature.Xml)
        {
        }
    }

    public class XmlReplyHandler : GenericHandler
    {
        public XmlReplyHandler()
            : base(MimeTypes.Xml, RequestAttributes.Reply | RequestAttributes.Xml, Feature.Xml)
        {
        }
    }
}