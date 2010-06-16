using RedisWebServices.DataSource.Northwind;
using RedisWebServices.ServiceModel.Operations.App;

namespace RedisWebServices.ServiceInterface.App
{
	public class PopulateRedisWithDataService
		: RedisServiceBase<PopulateRedisWithData>
	{
		static PopulateRedisWithDataService()
		{
			//NorthwindData.LoadData();
		}

		protected override object Run(PopulateRedisWithData request)
		{
			using (var client = base.ClientsManager.GetClient())
			{
				client.StoreAll(NorthwindData.Categories);
				client.StoreAll(NorthwindData.Customers);
				client.StoreAll(NorthwindData.Employees);
				client.StoreAll(NorthwindData.Shippers);
				client.StoreAll(NorthwindData.Orders);
				client.StoreAll(NorthwindData.Products);
				client.StoreAll(NorthwindData.OrderDetails);
				client.StoreAll(NorthwindData.CustomerCustomerDemos);
				client.StoreAll(NorthwindData.Regions);
				client.StoreAll(NorthwindData.Territories);
				client.StoreAll(NorthwindData.EmployeeTerritories);
			}

			return new PopulateRedisWithDataResponse();
		}
	}
}