using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.Common
{
	[DataContract]
	public class RemoveEntry
	{
		public RemoveEntry()
		{
			this.Keys = new List<string>();
		}

		[DataMember]
		public List<string> Keys { get; set; }
	}

	[DataContract]
	public class RemoveEntryResponse
	{
		public RemoveEntryResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember] 
		public bool Result { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}