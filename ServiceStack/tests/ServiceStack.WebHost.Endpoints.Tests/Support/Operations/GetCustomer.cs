using System;
using System.Runtime.Serialization;
using ServiceStack.WebHost.Endpoints.Tests.Support.Types;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Operations
{
	[DataContract]
	public class GetCustomer : IReturn<GetCustomerResponse>
	{
		[DataMember]
		public long CustomerId { get; set; }
	}

	[DataContract]
	public class GetCustomerResponse
	{
		[DataMember]
		public Customer Customer { get; set; }

        [DataMember]
        public DateTime Created { get; set; }
	}
}
