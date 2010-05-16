using System.Collections.Generic;
using System.Runtime.Serialization;
using RedisWebServices.ServiceModel.Types;
using ServiceStack.DesignPatterns.Model;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.Hash
{
	[DataContract]
	public class GetValuesFromHash
		: IHasStringId
	{
		[DataMember]
		public string Id { get; set; }

		[DataMember]
		public List<string> Keys { get; set; }
	}

	[DataContract]
	public class GetValuesFromHashResponse
	{
		public GetValuesFromHashResponse()
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