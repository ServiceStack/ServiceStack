using System;
using System.Runtime.Serialization;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.Common
{
	[DataContract]
	public class GetTimeToLive
	{
		[DataMember] 
		public string Key { get; set; }
	}

	[DataContract]
	public class GetTimeToLiveResponse
	{
		public GetTimeToLiveResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember] 
		public TimeSpan TimeRemaining { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}