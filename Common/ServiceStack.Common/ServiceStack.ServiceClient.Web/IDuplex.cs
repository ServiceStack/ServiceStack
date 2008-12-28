using System.ServiceModel;
using System.ServiceModel.Channels;

namespace ServiceStack.ServiceClient.Web
{
    [ServiceContract(Namespace = "http://ddn.services/facades", CallbackContract = typeof(IDuplexCallback))]
    public interface IDuplex
    {
        [OperationContract(Action = "*", ReplyAction = "*")]
        void BeginSend(Message msg);
    }
}