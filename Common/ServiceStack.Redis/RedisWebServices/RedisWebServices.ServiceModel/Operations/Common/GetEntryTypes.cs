using System.Runtime.Serialization;
using System.Collections.Generic;
using RedisWebServices.ServiceModel.Types;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.Common
{
	[DataContract]
	public class GetEntryTypes
	{
		public GetEntryTypes()
		{
			this.Keys = new List<string>();
		}

		[DataMember] 
		public List<string> Keys { get; set; }
	}

	[DataContract]
	public class GetEntryTypesResponse
	{
		public GetEntryTypesResponse()
		{
			this.ResponseStatus = new ResponseStatus();

			this.KeyTypes = new ArrayOfKeyValuePair();
		}

		[DataMember] 
		public ArrayOfKeyValuePair KeyTypes { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}