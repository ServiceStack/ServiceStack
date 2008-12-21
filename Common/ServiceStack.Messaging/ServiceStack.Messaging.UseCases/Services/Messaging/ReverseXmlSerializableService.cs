using ServiceStack.Common.Services.Serialization;
using ServiceStack.Messaging.UseCases.Objects.Serializable;
using ServiceStack.Messaging.UseCases.Services.Basic;

namespace ServiceStack.Messaging.UseCases.Services.Messaging
{
    public class ReverseXmlSerializableService : IService
    {
        #region IService Members

        public string Execute(IServiceHost serviceHost, ITextMessage message)
        {
            var request = new XmlSerializableDeserializer().Parse<XmlSerializableObject>(message.Text);
            var response = new XmlSerializableObject { Value = SimpleService.Reverse(request.Value) };
            var responseXml = new XmlSerializableSerializer().Parse(response);
            return responseXml;
        }

        #endregion
    }
}