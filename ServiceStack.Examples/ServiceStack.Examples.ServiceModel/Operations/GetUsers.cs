using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using ServiceStack.Examples.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;
using ArrayOfUser=ServiceStack.Examples.ServiceModel.Types.ArrayOfUser;

namespace ServiceStack.Examples.ServiceModel.Operations
{
	/// <summary>
	/// Use Plain old DataContract's Define your 'Service Interface'
	/// 
	/// This example shows the flavour of SOA-style webservices. 
	/// i.e. group similar operations into a single batch-full service request.
	/// </summary>
	[DataContract(Namespace = ExampleConfig.DefaultNamespace)]
	public class GetUsers
	{
		[DataMember]
		public List<long> UserIds { get; set; }

		[DataMember]
		public List<string> UserNames { get; set; }
	}

	[DataContract(Namespace = ExampleConfig.DefaultNamespace)]
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