using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.WebHost.Endpoints.Tests.Support.Operations;

namespace ServiceStack.WebHost.Endpoints.Tests.Support
{
	public abstract class MetadataTestBase
	{
		protected List<Type> ReplyOperations { get; set; }
		protected List<Type> OneWayOperations { get; set; }
		protected List<Type> AllOperations { get; set; }

		protected MetadataTestBase()
		{
			this.ReplyOperations = new[] { typeof(GetCustomer), typeof(GetCustomers) }.ToList();
			this.OneWayOperations = new[] { typeof(StoreCustomer) }.ToList();
			this.AllOperations = new[] { typeof(GetCustomerResponse), typeof(GetCustomersResponse) }.ToList();
			this.AllOperations.AddRange(ReplyOperations);
			this.AllOperations.AddRange(OneWayOperations);
		}
	}
}