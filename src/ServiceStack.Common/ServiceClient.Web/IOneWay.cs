#if !SILVERLIGHT && !MONOTOUCH && !XBOX
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace ServiceStack.ServiceClient.Web
{
    [ServiceContract(Namespace = "http://services.servicestack.net/")]
    public interface IOneWay
    {
        [OperationContract(Action = "*", IsOneWay = true)]
        void SendOneWay(Message requestMsg);
    }
}
#endif