using System;
using System.Collections.Generic;
using System.Data;
using ServiceStack.Common.Extensions;
using ServiceStack.OrmLite.Tests.Models;

namespace ServiceStack.OrmLite.TestsPerf.Scenarios.Northwind
{
	public class SelectNorthwindDataScenario
		: DatabaseScenarioBase
	{
		protected override void Run(IDbCommand dbCmd)
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