using System.ServiceModel;
using System.ServiceModel.Channels;

namespace ServiceStack.Common.Wcf
{
    [ServiceContract(Namespace = "http://servicestack.net/facades")]
    public interface ISyncReply
    {
        [OperationContract(Action = "*", ReplyAction = "*")]
        Message Send(Message msg);
    }
}