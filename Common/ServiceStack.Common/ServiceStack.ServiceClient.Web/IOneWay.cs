using System.ServiceModel;
using System.ServiceModel.Channels;

namespace ServiceStack.ServiceClient.Web
{
    [ServiceContract(Namespace = "http://ddn.services/facades")]
    public interface IOneWay
    {
        [OperationContract(Action = "*", IsOneWay = true)]
        void SendOneWay(Message msg);
    }
}