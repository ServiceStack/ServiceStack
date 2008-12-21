using ServiceStack.Common.Services.Serialization;
using ServiceStack.Messaging.Tests.Objects.Serializable;
using ServiceStack.Messaging.Tests.Services.Basic;

namespace ServiceStack.Messaging.Tests.Services.Messaging
{
    public class ReverseDataContractService : IService
    {
        public string Execute(IServiceHost serviceHost, ITextMessage message)
        {
            var request = new DataContractDeserializer().Parse<DataContractObject>(message.Text);
            var response = new DataContractObject { Value = SimpleService.Reverse(request.Value) };
            var responseXml = new DataContractSerializer().Parse(response);
            return responseXml;
        }
    }
}
