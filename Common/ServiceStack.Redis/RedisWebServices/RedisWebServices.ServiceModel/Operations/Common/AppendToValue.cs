using System.Runtime.Serialization;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.Common
{
	[DataContract]
	public class AppendToValue
	{
		[DataMember]
		public string Key { get; set; }

		[DataMember]
		public string Value { get; set; }
	}

	[DataContract]
	public class AppendToValueResponse
	{
		public AppendToValueResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember] 
		public int ValueLength { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}