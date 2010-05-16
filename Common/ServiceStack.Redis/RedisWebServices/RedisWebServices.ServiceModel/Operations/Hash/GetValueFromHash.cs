using System.Collections.Generic;
using System.Runtime.Serialization;
using RedisWebServices.ServiceModel.Types;
using ServiceStack.DesignPatterns.Model;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.Hash
{
	[DataContract]
	public class GetValueFromHash
		: IHasStringId
	{
		[DataMember]
		public string Id { get; set; }

		[DataMember]
		public string Key { get; set; }
	}

	[DataContract]
	public class GetValueFromHashResponse
	{
		public GetValueFromHashResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public string Value { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}