using System;
using System.Runtime.Serialization;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
	[DataContract]
	[RestService("/wildcard/{Id}/{Path}/{Action}")]
	[RestService("/wildcard/{Id}/{RemainingPath*}")]
	public class WildCardRequest
	{
		[DataMember]
		public int Id { get; set; }

		[DataMember]
		public string Path { get; set; }

		[DataMember]
		public string RemainingPath { get; set; }

		[DataMember]
		public string Action { get; set; }
	}

	[DataContract]
	public class WildCardRequestResponse
	{
		[DataMember]
		public string Result { get; set; }
	}

	public class WildCardRequestService 
		: ServiceBase<WildCardRequest>
	{
		protected override object Run(WildCardRequest request)
		{
			return request;
		}
	}
}