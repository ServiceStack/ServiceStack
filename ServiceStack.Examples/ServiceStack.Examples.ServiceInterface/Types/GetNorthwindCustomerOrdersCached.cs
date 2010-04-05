using System;
using System.Runtime.Serialization;
using ServiceStack.ServiceInterface.ServiceModel;

namespace ServiceStack.Examples.ServiceInterface.Types
{
	[DataContract]
	public class GetNorthwindCustomerOrdersCached
	{
		[DataMember]
		public bool RefreshCache { get; set; }

		[DataMember]
		public string CustomerId { get; set; }
	}

	[DataContract]
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