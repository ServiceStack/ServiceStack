using System.Runtime.Serialization;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.App
{
	[DataContract]
	public class PopulateRedisWithData
	{
	}

	[DataContract]
	public class PopulateRedisWithDataResponse
	{
		public PopulateRedisWithDataResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}