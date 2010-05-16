using System.Runtime.Serialization;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.Admin
{
	[Service(EndpointAttributes.InternalNetworkAccess)]
	[DataContract]
	public class FlushAll
	{
	}

	[DataContract]
	public class FlushAllResponse
	{
		public FlushAllResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}