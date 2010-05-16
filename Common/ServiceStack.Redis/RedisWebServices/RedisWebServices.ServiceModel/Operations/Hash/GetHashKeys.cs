using System.Collections.Generic;
using System.Runtime.Serialization;
using RedisWebServices.ServiceModel.Types;
using ServiceStack.DesignPatterns.Model;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.Hash
{
	[DataContract]
	public class GetHashKeys
		: IHasStringId
	{
		[DataMember]
		public string Id { get; set; }
	}

	[DataContract]
	public class GetHashKeysResponse
	{
		public GetHashKeysResponse()
		{
			this.ResponseStatus = new ResponseStatus();

			this.Keys = new ArrayOfString();
		}

		[DataMember]
		public ArrayOfString Keys { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}