using System.Runtime.Serialization;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.List
{
	[DataContract]
	public class PopAndPushItemBetweenLists
	{
		[DataMember]
		public string FromListId { get; set; }

		[DataMember]
		public string ToListId { get; set; }
	}

	[DataContract]
	public class PopAndPushItemBetweenListsResponse
	{
		public PopAndPushItemBetweenListsResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember] 
		public string Item { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}