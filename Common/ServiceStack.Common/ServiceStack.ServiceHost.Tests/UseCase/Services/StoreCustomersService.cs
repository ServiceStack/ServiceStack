using System.Data;
using ServiceStack.OrmLite;
using ServiceStack.ServiceHost.Tests.UseCase.Operations;

namespace ServiceStack.ServiceHost.Tests.UseCase.Services
{
	public class StoreCustomersService
		: IService<StoreCustomers>
	{
		private readonly IDbConnection dbConn;

		public StoreCustomersService(IDbConnection dbConn)
		{
			this.dbConn = dbConn;
		}

		public object Execute(StoreCustomers request)
		{
			using (var dbCmd = dbConn.CreateCommand())
			{
				dbCmd.CreateTable<Customer>(false);

				foreach (var customer in request.Customers)
				{
					dbCmd.Insert(customer);
				}
			}

			return null;
		}
	}

}