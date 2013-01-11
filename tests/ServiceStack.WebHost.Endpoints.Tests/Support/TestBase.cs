using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Tests.Support.Operations;

namespace ServiceStack.WebHost.Endpoints.Tests.Support
{
	public abstract class MetadataTestBase
	{
        public ServiceMetadata Metadata { get; set; }

		protected MetadataTestBase()
		{
            Metadata = new ServiceMetadata();
		    var dummyServiceType = GetType();
            Metadata.Add(dummyServiceType, typeof(GetCustomer), typeof(GetCustomerResponse));
            Metadata.Add(dummyServiceType, typeof(GetCustomers), typeof(GetCustomersResponse));
            Metadata.Add(dummyServiceType, typeof(StoreCustomer), null);
		}
	}
}