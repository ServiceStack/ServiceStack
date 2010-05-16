using System.Runtime.Serialization;
using ServiceStack.DesignPatterns.Model;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.Set
{
	[DataContract]
	public class GetSetCount
		: IHasStringId
	{
		[DataMember] 
		public string Id { get; set; }
	}

	[DataContract]
	public class GetSetCountResponse
	{
		public GetSetCountResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember] 
		public int Count { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}