using System;
using System.Runtime.Serialization;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
	[Route("/throwsargumentnull")]
	[DataContract]
	public class ThrowsArgumentNull
	{
		[DataMember]
		public string Value { get; set; }
	}

	[DataContract]
	public class ThrowsArgumentNullResponse
		: IHasResponseStatus
	{
		public ThrowsArgumentNullResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public string Result { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}

	public class ThrowsArgumentNullService 
		: ServiceBase<ThrowsArgumentNull>
	{
		protected override object Run(ThrowsArgumentNull request)
		{
			throw new ArgumentNullException("Name");
		}
	}
}