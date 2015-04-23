using System;
using ServiceStack.WebHost.Endpoints.Tests.Support.Operations;
using ServiceStack.WebHost.Endpoints.Tests.Support.Types;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
	public class GetCustomerService
		: TestServiceBase<GetCustomer>
	{
		protected override object Run(GetCustomer request)
		{
			return new GetCustomerResponse
			{
				Customer = new Customer
				{
					Id = request.CustomerId
				},
                Created = DateTime.UtcNow,
			};
		}
	}

}