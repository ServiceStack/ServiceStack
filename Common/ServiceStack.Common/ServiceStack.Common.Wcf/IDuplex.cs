using System.ServiceModel;
using System.ServiceModel.Channels;

namespace ServiceStack.Common.Wcf
{
    [ServiceContract(Namespace = "http://servicestack.net/facades", CallbackContract = typeof(IDuplexCallback))]
    public interface IDuplex
    {
        [OperationContract(Action = "*", ReplyAction = "*")]
        void BeginSend(Message msg);
    }
}