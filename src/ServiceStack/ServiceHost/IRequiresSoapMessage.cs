using System.ServiceModel.Channels;

namespace ServiceStack.ServiceHost
{
    public interface IRequiresSoapMessage
    {
        Message Message { get; set; }
    }
}