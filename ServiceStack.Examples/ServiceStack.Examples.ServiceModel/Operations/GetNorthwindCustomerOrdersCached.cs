using System;
using System.Runtime.Serialization;
using ServiceStack.ServiceInterface.ServiceModel;
using CustomerOrders=ServiceStack.Examples.ServiceModel.Types.CustomerOrders;

namespace ServiceStack.Examples.ServiceModel.Operations
{
	[DataContract(Namespace = ExampleConfig.DefaultNamespace)]
	public class GetNorthwindCustomerOrdersCached
	{
		[DataMember]
		public bool RefreshCache { get; set; }

		[DataMember]
		public string CustomerId { get; set; }
	}

	[DataContract(Namespace = ExampleConfig.DefaultNamespace)]
	public class GetNorthwindCustomerOrdersCachedResponse
	{
		public GetNorthwindCustomerOrdersCachedResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public DateTime CreatedDate { get; set; }

		[DataMember]
		public CustomerOrders CustomerOrders { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}