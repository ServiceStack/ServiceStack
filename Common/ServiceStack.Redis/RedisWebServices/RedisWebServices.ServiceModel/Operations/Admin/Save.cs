using System.Runtime.Serialization;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.Admin
{
	[Service(EndpointAttributes.InternalNetworkAccess)]
	[DataContract]
	public class Save
	{
		[DataMember]
		public bool InBackground { get; set; }
	}

	[DataContract]
	public class SaveResponse
	{
		public SaveResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}