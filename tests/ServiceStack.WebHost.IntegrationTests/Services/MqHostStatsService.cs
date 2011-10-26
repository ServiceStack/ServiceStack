using System;
using System.Runtime.Serialization;
using ServiceStack.Messaging;
using ServiceStack.ServiceInterface;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
	[DataContract]
	public class MqHostStats
	{
		[DataMember]
		public string Value { get; set; }
	}

	[DataContract]
	public class MqHostStatsResponse
	{
		[DataMember]
		public string Result { get; set; }
	}

	public class MqHostStatsService 
		: ServiceBase<MqHostStats>
	{
		public IMessageService MessageService { get; set; }

		protected override object Run(MqHostStats request)
		{
			return new MqHostStatsResponse { Result = MessageService.GetStatsDescription() };
		}
	}

}