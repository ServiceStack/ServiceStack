using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.Common
{
	[DataContract]
	public class GetValues
	{
		public GetValues()
		{
			this.Keys = new List<string>();
		}

		[DataMember]
		public List<string> Keys { get; set; }
	}

	[DataContract]
	public class GetValuesResponse
	{
		public GetValuesResponse()
		{
			this.ResponseStatus = new ResponseStatus();

			this.Values = new ArrayOfString();
		}

		[DataMember]
		public ArrayOfString Values { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}