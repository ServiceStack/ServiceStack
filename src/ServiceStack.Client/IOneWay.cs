#if !(SL5 || __IOS__ || XBOX || ANDROID || PCL)
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace ServiceStack
{
    [ServiceContract(Namespace = "http://services.servicestack.net/")]
    public interface IOneWay
    {
        [OperationContract(Action = "*", IsOneWay = true)]
        void SendOneWay(Message requestMsg);
    }
}
#endif