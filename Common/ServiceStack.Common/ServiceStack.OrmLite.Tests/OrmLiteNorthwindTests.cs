using System.Data;
using System.IO;
using NUnit.Framework;
using ServiceStack.OrmLite.Tests.Models;

namespace ServiceStack.OrmLite.Tests
{
	[TestFixture]
	public class OrmLiteNorthwindTests
		: OrmLiteTestBase
	{
		public static void CreateNorthwindTables(IDbCommand dbCmd)
		{
			dbCmd.CreateTables
			(
				 true,
				 typeof(Employee),
				 typeof(Category),
				 typeof(Customer),
				 typeof(Shipper),
				 typeof(Supplier),
				 typeof(Order),
				 typeof(Product),
				 typeof(OrderDetail),
				 typeof(CustomerCustomerDemo),
				 typeof(Category),
				 typeof(CustomerDemographic),
				 typeof(Region),
				 typeof(Territory),
				 typeof(EmployeeTerritory)
			);
		}

		private static void LoadNorthwindData(IDbCommand dbCmd)
		{
			NorthwindData.Categories.ForEach(x => dbCmd.Insert(x));
			NorthwindData.Customers.ForEach(x => dbCmd.Insert(x));
			NorthwindData.Employees.ForEach(x => dbCmd.Insert(x));
			NorthwindData.Shippers.ForEach(x => dbCmd.Insert(x));
			NorthwindData.Orders.ForEach(x => dbCmd.Insert(x));
			NorthwindData.Products.ForEach(x => dbCmd.Insert(x));
			NorthwindData.OrderDetails.ForEach(x => dbCmd.Insert(x));
			NorthwindData.CustomerCustomerDemos.ForEach(x => dbCmd.Insert(x));
			NorthwindData.Regions.ForEach(x => dbCmd.Insert(x));
			NorthwindData.Territories.ForEach(x => dbCmd.Insert(x));
			NorthwindData.EmployeeTerritories.ForEach(x => dbCmd.Insert(x));
		}

		[Test]
		public void Can_create_all_Northwind_tables()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbCmd = db.CreateCommand())
			{
				CreateNorthwindTables(dbCmd);
			}
		}

		[Test]
		public void Can_insert_Northwind_Data()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbCmd = db.CreateCommand())
			{
				CreateNorthwindTables(dbCmd);

				NorthwindData.LoadData(false);
				LoadNorthwindData(dbCmd);
			}
		}

		[Test]
		public void Can_insert_Northwind_Data_with_images()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbCmd = db.CreateCommand())
			{
				CreateNorthwindTables(dbCmd);

				NorthwindData.LoadData(true);
				LoadNorthwindData(dbCmd);
			}
		}

	}
}