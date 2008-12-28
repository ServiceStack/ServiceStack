using System.ServiceModel;
using System.ServiceModel.Channels;

namespace ServiceStack.ServiceClient.Web
{
    [ServiceContract(Namespace = "http://ddn.services/facades")]
    public interface IDuplexCallback
    {
        [OperationContract(Action = "*", ReplyAction = "*")]
        void OnMessageReceived(Message msg);
    }
}