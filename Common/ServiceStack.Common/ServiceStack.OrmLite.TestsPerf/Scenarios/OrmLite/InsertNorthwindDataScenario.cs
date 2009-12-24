using System;
using System.Data;
using ServiceStack.Common.Extensions;
using ServiceStack.OrmLite.Tests.Models;

namespace ServiceStack.OrmLite.TestsPerf.Scenarios.OrmLite
{
	public class InsertNorthwindDataScenario
		: DatabaseScenarioBase
	{
		protected override void Run(IDbCommand dbCmd)
		{
			if (this.IsFirstRun)
			{
				dbCmd.CreateTables(true,
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

			NorthwindData.LoadImages = false;
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
	}

}