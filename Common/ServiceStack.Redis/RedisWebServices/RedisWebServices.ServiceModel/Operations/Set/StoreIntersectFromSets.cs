using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack.DesignPatterns.Model;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.Set
{
	[DataContract]
	public class StoreIntersectFromSets
		: IHasStringId
	{
		public StoreIntersectFromSets()
		{
			this.SetIds = new List<string>();
		}

		[DataMember]
		public string Id { get; set; }

		[DataMember]
		public List<string> SetIds { get; set; }
	}

	[DataContract]
	public class StoreIntersectFromSetsResponse
	{
		public StoreIntersectFromSetsResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}