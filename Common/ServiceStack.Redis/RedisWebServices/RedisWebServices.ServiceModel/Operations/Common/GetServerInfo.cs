using System.Collections.Generic;
using System.Runtime.Serialization;
using RedisWebServices.ServiceModel.Types;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.Common
{
	[DataContract]
	public class GetServerInfo
	{
	}

	[DataContract]
	public class GetServerInfoResponse
	{
		public GetServerInfoResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public ArrayOfKeyValuePair ServerInfo { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}