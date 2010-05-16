using System.Collections.Generic;
using System.Runtime.Serialization;
using RedisWebServices.ServiceModel.Types;
using ServiceStack.DesignPatterns.Model;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.Hash
{
	[DataContract]
	public class GetAllEntriesFromHash
		: IHasStringId
	{
		[DataMember]
		public string Id { get; set; }
	}

	[DataContract]
	public class GetAllEntriesFromHashResponse
	{
		public GetAllEntriesFromHashResponse()
		{
			this.ResponseStatus = new ResponseStatus();

			this.KeyValuePairs = new ArrayOfKeyValuePair();
		}

		[DataMember]
		public ArrayOfKeyValuePair KeyValuePairs { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}