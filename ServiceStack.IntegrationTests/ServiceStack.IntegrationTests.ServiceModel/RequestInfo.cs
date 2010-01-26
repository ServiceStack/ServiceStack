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
			this.NetworkAttributes = new Dictionary<string, string>();
		}

		[DataMember]
		public List<string> EnpointAttributes { get; set; }

		[DataMember]
		public List<string> RequestAttributes { get; set; }

		[DataMember]
		public string IpAddress { get; set; }

		[DataMember]
		public Dictionary<string, string> NetworkAttributes { get; set; }

		[DataMember]
		public string NetworkLog { get; set; }
	}

}