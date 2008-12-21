using ServiceStack.Common.Services.Serialization;
using ServiceStack.Messaging.UseCases.Objects.Serializable;
using ServiceStack.Messaging.UseCases.Services.Basic;

namespace ServiceStack.Messaging.UseCases.Services.Messaging
{
    public class ReverseDataContractService : IService
    {
        public string Execute(IServiceHost serviceHost, ITextMessage message)
        {
            var request = new DataContractDeserializer().Parse<DataContractObject>(message.Text);
            var response = new DataContractObject { Value = SimpleService.Reverse(request.Value) };
            string responseXml = new DataContractSerializer().Parse(response);
            return responseXml;
        }
    }
}