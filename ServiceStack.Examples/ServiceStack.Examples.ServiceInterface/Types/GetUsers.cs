using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace ServiceStack.Examples.ServiceInterface.Types
{
	/// <summary>
	/// Use Plain old DataContract's Define your 'Service Interface'
	/// 
	/// This example shows a simple introduction into SOA-like webservices. 
	/// i.e. group similar operations into a single 'document-centric like' service request.
	/// </summary>
	[DataContract(Namespace = "http://schemas.sericestack.net/examples/types")]
	public class GetUsers
	{
		[DataMember]
		public List<long> UserIds { get; set; }

		[DataMember]
		public List<string> UserNames { get; set; }
	}

	[DataContract(Namespace = "http://schemas.sericestack.net/examples/types")]
	public class GetUsersResponse
	{
		public GetUsersResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public ArrayOfUser Users { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}