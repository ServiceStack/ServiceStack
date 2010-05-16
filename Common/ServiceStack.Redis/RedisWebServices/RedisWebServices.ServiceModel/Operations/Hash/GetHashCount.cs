using System.Collections.Generic;
using System.Runtime.Serialization;
using RedisWebServices.ServiceModel.Types;
using ServiceStack.DesignPatterns.Model;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.Hash
{
	[DataContract]
	public class GetHashCount
		: IHasStringId
	{
		[DataMember]
		public string Id { get; set; }
	}

	[DataContract]
	public class GetHashCountResponse
	{
		public GetHashCountResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public int Count { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}