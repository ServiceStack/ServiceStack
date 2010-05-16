using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.Messaging
{
	[DataContract]
	public class CreateSubscription
	{
		public CreateSubscription()
		{
			this.Channels = new List<string>();
			this.Patterns = new List<string>();
		}

		[DataMember]
		public List<string> Channels { get; set; }

		[DataMember]
		public List<string> Patterns { get; set; }

		[DataMember]
		public TimeSpan? TimeOut { get; set; }
	}

	[DataContract]
	public class CreateSubscriptionResponse
	{
		public CreateSubscriptionResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public string Key { get; set; }

		[DataMember]
		public string Message { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}