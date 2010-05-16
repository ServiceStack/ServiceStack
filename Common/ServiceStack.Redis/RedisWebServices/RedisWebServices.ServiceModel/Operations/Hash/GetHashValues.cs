using System.Collections.Generic;
using System.Runtime.Serialization;
using RedisWebServices.ServiceModel.Types;
using ServiceStack.DesignPatterns.Model;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.Hash
{
	[DataContract]
	public class GetHashValues
		: IHasStringId
	{
		[DataMember]
		public string Id { get; set; }
	}

	[DataContract]
	public class GetHashValuesResponse
	{
		public GetHashValuesResponse()
		{
			this.ResponseStatus = new ResponseStatus();

			this.Values = new ArrayOfString();
		}

		[DataMember]
		public ArrayOfString Values { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}