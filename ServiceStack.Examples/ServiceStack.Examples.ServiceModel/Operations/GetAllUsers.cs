using System.Runtime.Serialization;
using ServiceStack.Examples.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;
using ArrayOfUser=ServiceStack.Examples.ServiceModel.Types.ArrayOfUser;

namespace ServiceStack.Examples.ServiceModel.Operations
{
	/// <summary>
	/// Use Plain old DataContract's Define your 'Service Interface'
	/// 
	/// This example shows a simple introduction into SOA-like webservices. 
	/// i.e. group similar operations into a single 'document-centric like' service request.
	/// </summary>
	[DataContract(Namespace = ExampleConfig.DefaultNamespace)]
	public class GetAllUsers
	{
	}

	[DataContract(Namespace = ExampleConfig.DefaultNamespace)]
	public class GetAllUsersResponse
	{
		public GetAllUsersResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public ArrayOfUser Users { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}