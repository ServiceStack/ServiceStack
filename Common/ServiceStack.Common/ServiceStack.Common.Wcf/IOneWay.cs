using System.ServiceModel;
using System.ServiceModel.Channels;

namespace ServiceStack.Common.Wcf
{
	[ServiceContract(Namespace = "http://servicestack.net/facades")]
	public interface IOneWay
    {
        [OperationContract(Action = "*", IsOneWay = true)]
        void SendOneWay(Message msg);
    }
}