using System.Runtime.Serialization;
using ServiceStack.ServiceInterface.ServiceModel;

namespace ServiceStack.WebHost.IntegrationTests.Operations
{
	[DataContract]
	public class AlwaysThrows
	{
		[DataMember]
		public string Value { get; set; }
	}

	[DataContract]
	public class AlwaysThrowsResponse
		: IHasResponseStatus
	{
		public AlwaysThrowsResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public string Result { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}