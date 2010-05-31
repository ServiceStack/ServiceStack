using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack.DesignPatterns.Model;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.Set
{
	[DataContract]
	public class AddRangeToSet
		: IHasStringId
	{
		public AddRangeToSet()
		{
			this.Items = new List<string>();
		}

		[DataMember] 
		public string Id { get; set; }

		[DataMember]
		public List<string> Items { get; set; }
	}

	[DataContract]
	public class AddRangeToSetResponse
	{
		public AddRangeToSetResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}