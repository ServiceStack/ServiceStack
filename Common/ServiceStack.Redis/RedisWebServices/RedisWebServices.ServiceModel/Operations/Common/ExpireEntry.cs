using System;
using System.Runtime.Serialization;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.Common
{
	[DataContract]
	public class ExpireEntry
	{
		[DataMember]
		public string Key { get; set; }

		[DataMember]
		public TimeSpan? ExpireIn { get; set; }

		[DataMember]
		public DateTime? ExpireAt { get; set; }
	}

	[DataContract]
	public class ExpireEntryResponse
	{
		public ExpireEntryResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember] 
		public bool Result { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}