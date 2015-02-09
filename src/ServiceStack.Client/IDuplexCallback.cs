#if !(SL5 || __IOS__ || XBOX || ANDROID || PCL)
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace ServiceStack
{
    [ServiceContract(Namespace = "http://services.servicestack.net/")]
    public interface IDuplexCallback
    {
        [OperationContract(Action = "*", ReplyAction = "*")]
        void OnMessageReceived(Message msg);
    }
}
#endif