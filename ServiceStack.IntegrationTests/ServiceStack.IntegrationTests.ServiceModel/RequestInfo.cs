using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ServiceStack.IntegrationTests.ServiceModel
{
	[DataContract]
	public class RequestInfo
	{
	}

	[DataContract]
	public class RequestInfoResponse
	{
		public RequestInfoResponse()
		{
			this.EnpointAttributes = new List<string>();
			this.RequestAttributes = new List<string>();
			this.Ipv4Addresses = new Dictionary<string, string>();
			this.Ipv6Addresses = new List<string>();
		}

		[DataMember]
		public List<string> EnpointAttributes { get; set; }

		[DataMember]
		public List<string> RequestAttributes { get; set; }

		[DataMember]
		public string IpAddress { get; set; }
		public string IpAddressFamily { get; set; }

		[DataMember]
		public Dictionary<string, string> Ipv4Addresses { get; set; }

		[DataMember]
		public List<string> Ipv6Addresses { get; set; }

		[DataMember]
		public string NetworkLog { get; set; }
	}

}