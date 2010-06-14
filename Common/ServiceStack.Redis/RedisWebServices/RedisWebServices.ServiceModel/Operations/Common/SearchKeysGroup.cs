using System.Runtime.Serialization;
using RedisWebServices.ServiceModel.Types;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.Common
{
	[DataContract]
	public class SearchKeysGroup
	{
		[DataMember] 
		public string Pattern { get; set; }
	}

	[DataContract]
	public class SearchKeysGroupResponse
	{
		public SearchKeysGroupResponse()
		{
			this.ResponseStatus = new ResponseStatus();

			this.KeyGroups = new ArrayOfItemWithScore();
		}

		[DataMember]
		public ArrayOfItemWithScore KeyGroups { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}