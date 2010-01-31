using System;
using System.Collections.Generic;
using System.Linq;
using Northwind.Common.DataModel;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Common.Utils;

namespace ServiceStack.Redis.Tests.Generic
{
	[TestFixture]
	public class RedisClientTests
	{
		[TestFixtureSetUp]
		public void TestFixture()
		{
			NorthwindData.LoadData(false);
			using (var redis = new RedisClient())
			{
				redis.FlushAll();
			}
		}

		[Test]
		public void Can_Set_and_Get_string()
		{
			const string value = "value";
			using (var redis = new RedisClient())
			{
				redis.SetString("key", value);
				var valueString = redis.GetString("key");

				Assert.That(valueString, Is.EqualTo(value));
			}
		}

		public List<T> Sort<T>(IEnumerable<T> list)
		{
			var sortedList = list.ToList();
			sortedList.Sort((x, y) => 
				x.GetId().ToString().CompareTo(y.GetId().ToString()));

			return sortedList;
		}

		public void AssertUnorderedListsAreEqual<T>(IList<T> actualList, IList<T> expectedList)
		{
			Assert.That(actualList, Has.Count(expectedList.Count));

			var actualMap = Sort(actualList.Select(x => x.GetId()));
			var expectedMap = Sort(expectedList.Select(x => x.GetId()));

			Assert.That(actualMap, Is.EquivalentTo(expectedMap));
		}
		

		[Test]
		public void Can_StoreAll_and_GetAll_from_Northwind()
		{
			var totalRecords
				= NorthwindData.Categories.Count
				  + NorthwindData.Customers.Count
				  + NorthwindData.Employees.Count
				  + NorthwindData.Shippers.Count
				  + NorthwindData.Orders.Count
				  + NorthwindData.OrderDetails.Count
				  + NorthwindData.CustomerCustomerDemos.Count
				  + NorthwindData.Regions.Count
				  + NorthwindData.Territories.Count
				  + NorthwindData.EmployeeTerritories.Count;

			using (var client = new RedisClient())
			{
				var before = DateTime.Now;

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

				Console.WriteLine("Took {0}ms to store the entire Northwind database ({1} records)",
					(DateTime.Now - before).TotalMilliseconds, totalRecords);


				before = DateTime.Now;

				var categories = client.GetAll<Category>();
				var customers = client.GetAll<Customer>();
				var employees = client.GetAll<Employee>();
				var shippers = client.GetAll<Shipper>();
				var orders = client.GetAll<Order>();
				var products = client.GetAll<Product>();
				var orderDetails = client.GetAll<OrderDetail>();
				var customerCustomerDemos = client.GetAll<CustomerCustomerDemo>();
				var regions = client.GetAll<Region>();
				var territories = client.GetAll<Territory>();
				var employeeTerritories = client.GetAll<EmployeeTerritory>();

				Console.WriteLine("Took {0}ms to get the entire Northwind database ({1} records)",
					(DateTime.Now - before).TotalMilliseconds, totalRecords);


				AssertUnorderedListsAreEqual(categories, NorthwindData.Categories);
				AssertUnorderedListsAreEqual(customers, NorthwindData.Customers);
				AssertUnorderedListsAreEqual(employees, NorthwindData.Employees);
				AssertUnorderedListsAreEqual(shippers, NorthwindData.Shippers);
				AssertUnorderedListsAreEqual(orders, NorthwindData.Orders);
				AssertUnorderedListsAreEqual(products, NorthwindData.Products);
				AssertUnorderedListsAreEqual(orderDetails, NorthwindData.OrderDetails);
				AssertUnorderedListsAreEqual(customerCustomerDemos, NorthwindData.CustomerCustomerDemos);
				AssertUnorderedListsAreEqual(regions, NorthwindData.Regions);
				AssertUnorderedListsAreEqual(territories, NorthwindData.Territories);
				AssertUnorderedListsAreEqual(employeeTerritories, NorthwindData.EmployeeTerritories);
			}
		}

	}
}
