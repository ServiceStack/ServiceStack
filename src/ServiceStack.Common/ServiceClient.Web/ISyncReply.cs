#if !SILVERLIGHT && !MONOTOUCH && !XBOX
using System.ServiceModel;
using System.ServiceModel.Channels;
using ServiceStack.ServiceHost;

namespace ServiceStack.ServiceClient.Web
{
    [ServiceContract(Namespace = "http://services.servicestack.net/")]
    public interface ISyncReply
    {
        [OperationContract(Action = "*", ReplyAction = "*")]
        Message Send(Message requestMsg, IHttpRequest httpRequest, IHttpResponse httpResponse);
    }
}
#endif