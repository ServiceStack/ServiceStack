using System.Runtime.Serialization;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.Admin
{
	//[Service(EndpointAttributes.InternalNetworkAccess)]
	[DataContract]
	public class RewriteAppendOnlyFileAsync
	{
	}

	[DataContract]
	public class RewriteAppendOnlyFileAsyncResponse
	{
		public RewriteAppendOnlyFileAsyncResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}