using System;
using System.Data;
using ServiceStack.OrmLite;
using ServiceStack.ServiceHost.Tests.UseCase.Operations;

namespace ServiceStack.ServiceHost.Tests.UseCase.Services
{
	public class GetCustomerService
		: IService<GetCustomer>
	{
		private readonly IDbConnection dbConn;

		public GetCustomerService(IDbConnection dbConn)
		{
			this.dbConn = dbConn;
		}

		public object Execute(GetCustomer request)
		{
			using (var dbCmd = dbConn.CreateCommand())
			{
				return new GetCustomerResponse {
					Customer = dbCmd.GetById<Customer>(request.CustomerId)
				};
			}
		}
	}
}