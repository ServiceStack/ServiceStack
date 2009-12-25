using System;
using System.Collections.Generic;
using System.Data;
using ServiceStack.Common.Extensions;
using ServiceStack.OrmLite.Tests.Models;

namespace ServiceStack.OrmLite.TestsPerf.Scenarios.Northwind
{
	public class InsertNorthwindDataScenario
		: DatabaseScenarioBase
	{
		private static readonly List<Type> ModelTypes = new List<Type> {
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
			typeof(EmployeeTerritory),
		};

		protected override void Run(IDbCommand dbCmd)
		{
			InsertData(dbCmd);
		}

		public void InsertData(IDbCommand dbCmd)
		{
			if (this.IsFirstRun)
			{
				dbCmd.CreateTables(true, ModelTypes.ToArray());
			}
			else
			{
				ModelTypes.ForEach(x => dbCmd.DeleteAll(x));
			}

			NorthwindData.LoadData(false);
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