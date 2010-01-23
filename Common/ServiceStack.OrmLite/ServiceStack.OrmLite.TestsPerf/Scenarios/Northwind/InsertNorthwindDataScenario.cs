using System;
using System.Collections.Generic;
using System.Data;
using Northwind.Common.DataModel;
using Northwind.Perf;

namespace ServiceStack.OrmLite.TestsPerf.Scenarios.Northwind
{
	public class InsertNorthwindDataScenario
		: DatabaseScenarioBase
{
		static InsertNorthwindDataScenario()
		{
			NorthwindData.LoadData(false);
		}

		protected override void Run(IDbCommand dbCmd)
		{
			InsertData(dbCmd);
		}

		public void InsertData(IDbCommand dbCmd)
		{
			if (this.IsFirstRun)
			{
				dbCmd.CreateTables(true, NorthwindFactory.ModelTypes.ToArray());
			}
			else
			{
				NorthwindFactory.ModelTypes.ForEach(x => dbCmd.DeleteAll(x));
			}

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