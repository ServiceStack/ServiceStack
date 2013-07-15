#if !SILVERLIGHT && !MONOTOUCH && !XBOX && !ANDROIDINDIE
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace ServiceStack.ServiceClient.Web
{
    [ServiceContract(Namespace = "http://services.servicestack.net/")]
    public interface IDuplexCallback
    {
        [OperationContract(Action = "*", ReplyAction = "*")]
        void OnMessageReceived(Message msg);
    }
}
#endif