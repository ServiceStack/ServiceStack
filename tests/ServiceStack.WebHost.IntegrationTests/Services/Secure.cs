using System;
using System.Runtime.Serialization;
using ServiceStack.ServiceInterface;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
	public interface IRequiresSession
	{
		Guid SessionId { get; }
	}

	[DataContract]
	public class Secure : IRequiresSession
	{
		[DataMember]
		public Guid SessionId { get; set;}

		[DataMember]
		public int StatusCode { get; set; }
	}

	[DataContract]
	public class SecureResponse
	{
		[DataMember]
		public string Value { get; set; }
	}

	public class SecureService : ServiceInterface.Service
	{
        public object Any(Secure request)
		{
			throw new UnauthorizedAccessException("You shouldn't be able to see this");
		}
	}
}