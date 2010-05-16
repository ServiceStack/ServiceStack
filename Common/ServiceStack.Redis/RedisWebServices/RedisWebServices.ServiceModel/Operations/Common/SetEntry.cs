using System;
using System.Runtime.Serialization;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.Common
{
	[DataContract]
	public class SetEntry
	{
		[DataMember]
		public string Key { get; set; }

		[DataMember]
		public string Value { get; set; }

		[DataMember]
		public TimeSpan? ExpireIn { get; set; }
	}

	[DataContract]
	public class SetEntryResponse
	{
		public SetEntryResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}