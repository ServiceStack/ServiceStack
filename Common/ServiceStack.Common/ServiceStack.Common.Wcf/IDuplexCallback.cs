using System.ServiceModel;
using System.ServiceModel.Channels;

namespace ServiceStack.Common.Wcf
{
	[ServiceContract(Namespace = "http://servicestack.net/facades")]
	public interface IDuplexCallback
    {
        [OperationContract(Action = "*", ReplyAction = "*")]
        void OnMessageReceived(Message msg);
    }
}