using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack.DesignPatterns.Model;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.List
{
	[DataContract]
	public class AddRangeToList
		: IHasStringId
	{
		public AddRangeToList()
		{
			this.Items = new List<string>();
		}

		[DataMember]
		public string Id { get; set; }

		[DataMember]
		public List<string> Items { get; set; }
	}

	[DataContract]
	public class AddRangeToListResponse
	{
		public AddRangeToListResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}