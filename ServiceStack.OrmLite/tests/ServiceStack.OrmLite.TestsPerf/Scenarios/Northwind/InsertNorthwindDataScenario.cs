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

        protected override void Run(IDbConnection db)
		{
			InsertData(db);
		}

        public void InsertData(IDbConnection db)
		{
			if (this.IsFirstRun)
			{
				db.CreateTables(true, NorthwindFactory.ModelTypes.ToArray());
			}
			else
			{
				NorthwindFactory.ModelTypes.ForEach(x => db.DeleteAll(x));
			}

			NorthwindData.Categories.ForEach(x => db.Insert(x));
			NorthwindData.Customers.ForEach(x => db.Insert(x));
			NorthwindData.Employees.ForEach(x => db.Insert(x));
			NorthwindData.Shippers.ForEach(x => db.Insert(x));
			NorthwindData.Orders.ForEach(x => db.Insert(x));
			NorthwindData.Products.ForEach(x => db.Insert(x));
			NorthwindData.OrderDetails.ForEach(x => db.Insert(x));
			NorthwindData.CustomerCustomerDemos.ForEach(x => db.Insert(x));
			NorthwindData.Regions.ForEach(x => db.Insert(x));
			NorthwindData.Territories.ForEach(x => db.Insert(x));
			NorthwindData.EmployeeTerritories.ForEach(x => db.Insert(x));
		}
	}
}