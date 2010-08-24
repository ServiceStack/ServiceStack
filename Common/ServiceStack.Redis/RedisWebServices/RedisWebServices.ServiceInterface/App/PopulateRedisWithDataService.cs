using System.Collections.Generic;
using RedisWebServices.DataSource.Northwind;
using RedisWebServices.ServiceModel.Operations.App;
using ServiceStack.Redis;

namespace RedisWebServices.ServiceInterface.App
{
	public class PopulateRedisWithDataService
		: RedisServiceBase<PopulateRedisWithData>
	{
		static PopulateRedisWithDataService()
		{
			NorthwindData.LoadData();
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
				//client.StoreAll(NorthwindData.CustomerCustomerDemos);
				client.StoreAll(NorthwindData.Regions);
				client.StoreAll(NorthwindData.Territories);
				client.StoreAll(NorthwindData.EmployeeTerritories);

				LoadDifferentKeyTypes(client);
			}

			return new PopulateRedisWithDataResponse();
		}

		protected void LoadDifferentKeyTypes(IRedisClient client)
		{
			var items = new List<string> { "one", "two", "three", "four" };
			var map = new Dictionary<string, string> {
						{"A","one"},
						{"B","two"},
						{"C","three"},
						{"D","four"},
					};

			items.ForEach(x => client.Set("urn:testkeytypes:string:" + x, x));
			items.ForEach(x => client.AddItemToList("urn:testkeytypes:list", x));
			items.ForEach(x => client.AddItemToSet("urn:testkeytypes:set", x));
			var i = 0;
			items.ForEach(x => client.AddItemToSortedSet("urn:testkeytypes:zset", x, i++));
			client.SetRangeInHash("urn:testkeytypes:hash", map);
		}
	}
}