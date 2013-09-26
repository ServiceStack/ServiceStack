using System.Runtime.Serialization;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
	[DataContract]
	[Route("/wildcard/{Id}/{Path}/{Action}")]
	[Route("/wildcard/{Id}/{RemainingPath*}")]
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

    [Route("/path/{Tail*}")]
    public class BasicWildcard
    {
        public string Tail { get; set; }
    }
    
	public class WildCardRequestService : Service
	{
        public object Get(WildCardRequest request)
        {
            return request;
        }

        public object Get(BasicWildcard request)
        {
            return request;
        }
    }
}