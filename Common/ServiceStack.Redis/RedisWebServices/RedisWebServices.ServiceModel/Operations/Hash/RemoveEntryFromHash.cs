using System.Collections.Generic;
using System.Runtime.Serialization;
using RedisWebServices.ServiceModel.Types;
using ServiceStack.DesignPatterns.Model;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.Hash
{
	[DataContract]
	public class RemoveEntryFromHash
		: IHasStringId
	{
		[DataMember]
		public string Id { get; set; }

		[DataMember]
		public string Key { get; set; }
	}

	[DataContract]
	public class RemoveEntryFromHashResponse
	{
		public RemoveEntryFromHashResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public bool Result { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}