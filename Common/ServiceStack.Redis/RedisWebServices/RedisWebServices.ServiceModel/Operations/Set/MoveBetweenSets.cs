using System.Runtime.Serialization;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.Set
{
	[DataContract]
	public class MoveBetweenSets
	{
		[DataMember]
		public string FromSetId { get; set; }

		[DataMember]
		public string ToSetId { get; set; }

		[DataMember]
		public string Item { get; set; }
	}

	[DataContract]
	public class MoveBetweenSetsResponse
	{
		public MoveBetweenSetsResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}