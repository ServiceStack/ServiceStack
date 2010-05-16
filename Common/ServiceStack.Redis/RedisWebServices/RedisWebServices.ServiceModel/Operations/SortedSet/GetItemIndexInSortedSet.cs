using System.Runtime.Serialization;
using ServiceStack.DesignPatterns.Model;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.SortedSet
{
	[DataContract]
	public class GetItemIndexInSortedSet
		: IHasStringId
	{
		[DataMember]
		public string Id { get; set; }

		[DataMember]
		public string Item { get; set; }

		[DataMember]
		public bool SortDescending { get; set; }
	}

	[DataContract]
	public class GetItemIndexInSortedSetResponse
	{
		public GetItemIndexInSortedSetResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public int Index { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}