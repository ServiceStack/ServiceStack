using System.Runtime.Serialization;
using ServiceStack.DesignPatterns.Model;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.SortedSet
{
	[DataContract]
	public class GetSortedSetCount
		: IHasStringId
	{
		[DataMember]
		public string Id { get; set; }
	}

	[DataContract]
	public class GetSortedSetCountResponse
	{
		public GetSortedSetCountResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public int Count { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}