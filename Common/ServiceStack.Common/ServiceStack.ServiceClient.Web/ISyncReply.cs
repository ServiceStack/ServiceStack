using System.ServiceModel;
using System.ServiceModel.Channels;

namespace ServiceStack.ServiceClient.Web
{
    [ServiceContract(Namespace = "http://ddn.services/facades")]
    public interface ISyncReply
    {
        [OperationContract(Action = "*", ReplyAction = "*")]
        Message Send(Message msg);
    }
}