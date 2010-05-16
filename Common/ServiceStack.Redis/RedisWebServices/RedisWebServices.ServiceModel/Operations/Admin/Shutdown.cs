using System.Runtime.Serialization;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.Admin
{
	[Service(EndpointAttributes.InternalNetworkAccess)]
	[DataContract]
	public class Shutdown
	{
	}

	[DataContract]
	public class ShutdownResponse
	{
		public ShutdownResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}