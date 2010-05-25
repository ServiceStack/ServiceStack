using System;
using System.Runtime.Serialization;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.Common
{
	[DataContract]
	public class ExpireEntryIn
	{
		[DataMember]
		public string Key { get; set; }

		[DataMember]
		public TimeSpan ExpireIn { get; set; }
	}

	[DataContract]
	public class ExpireEntryInResponse
	{
		public ExpireEntryInResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember] 
		public bool Result { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}