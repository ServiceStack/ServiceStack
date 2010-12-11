using System;
using System.Runtime.Serialization;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;

namespace ServiceStack.WebHost.IntegrationTests.Services
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

	public class AlwaysThrowsService 
		: ServiceBase<AlwaysThrows>
	{
		protected override object Run(AlwaysThrows request)
		{
			throw new NotImplementedException(GetErrorMessage(request.Value));
		}

		public static string GetErrorMessage(string value)
		{
			return value + " is not implemented";
		}
	}
}