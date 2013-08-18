using System;
using System.Runtime.Serialization;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
	[DataContract]
	[Route("/echo/{Id}/{String}")]
	public class EchoRequest
	{
		[DataMember]
		public int Id { get; set; }

		[DataMember]
		public string String { get; set; }

		[DataMember]
		public long Long { get; set; }

		[DataMember]
		public Guid Guid { get; set; }

		[DataMember]
		public bool Bool { get; set; }

		[DataMember]
		public DateTime DateTime { get; set; }

		[DataMember]
		public double Double { get; set; }
	}

	[DataContract]
	public class EchoRequestResponse
	{
		[DataMember]
		public string Result { get; set; }
	}

	public class EchoRequestService : ServiceInterface.Service
	{
        public object Any(EchoRequest request)
		{
			return request;
		}
	}
}