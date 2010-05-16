using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.Set
{
	[DataContract]
	public class GetIntersectFromSets
	{
		public GetIntersectFromSets()
		{
			this.SetIds = new List<string>();
		}

		[DataMember] 
		public List<string> SetIds { get; set; }
	}

	[DataContract]
	public class GetIntersectFromSetsResponse
	{
		public GetIntersectFromSetsResponse()
		{
			this.ResponseStatus = new ResponseStatus();

			this.Items = new ArrayOfString();
		}

		[DataMember] 
		public ArrayOfString Items { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}