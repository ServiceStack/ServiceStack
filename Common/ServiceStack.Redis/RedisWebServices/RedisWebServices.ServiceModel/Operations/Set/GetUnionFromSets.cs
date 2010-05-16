using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.Set
{
	[DataContract]
	public class GetUnionFromSets
	{
		public GetUnionFromSets()
		{
			this.SetIds = new List<string>();
		}

		[DataMember]
		public List<string> SetIds { get; set; }
	}

	[DataContract]
	public class GetUnionFromSetsResponse
	{
		public GetUnionFromSetsResponse()
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