using System.Runtime.Serialization;
using ServiceStack.DesignPatterns.Model;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.List
{
	[DataContract]
	public class RemoveItemFromList
		: IHasStringId
	{
		[DataMember]
		public string Id { get; set; }

		[DataMember]
		public string Item { get; set; }

		[DataMember]
		public int? NoOfMatches { get; set; }
	}

	[DataContract]
	public class RemoveItemFromListResponse
	{
		public RemoveItemFromListResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public int ItemsRemovedCount { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}