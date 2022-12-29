using System;
using System.Collections.Generic;
using System.Data;
using Northwind.Common.DataModel;
using Northwind.Perf;
using ServiceStack.Common;

namespace ServiceStack.OrmLite.TestsPerf.Scenarios.Northwind
{
	public class SelectNorthwindDataScenario
		: DatabaseScenarioBase
	{
        protected override void Run(IDbConnection dbCmd)
		{
			if (this.IsFirstRun)
			{
				new InsertNorthwindDataScenario().InsertData(dbCmd);
			}

			dbCmd.Select<Category>();
			dbCmd.Select<Customer>();
			dbCmd.Select<Employee>();
			dbCmd.Select<Shipper>();
			dbCmd.Select<Order>();
			dbCmd.Select<Product>();
			dbCmd.Select<OrderDetail>();
			dbCmd.Select<CustomerCustomerDemo>();
			dbCmd.Select<Region>();
			dbCmd.Select<Territory>();
			dbCmd.Select<EmployeeTerritory>();
		}
	}
}