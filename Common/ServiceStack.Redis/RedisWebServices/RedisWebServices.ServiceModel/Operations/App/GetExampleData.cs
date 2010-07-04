using System.Runtime.Serialization;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.App
{
	[DataContract]
	public class GetExampleData
	{
	}

	[DataContract]
	public class GetExampleDataResponse
	{
		public GetExampleDataResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}