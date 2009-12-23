using System;
using System.Data;
using ServiceStack.Common.Extensions;
using ServiceStack.OrmLite.Tests.Models;

namespace ServiceStack.OrmLite.TestsPerf.Scenarios.OrmLite
{
	public class InsertNorthwindSampleDataScenario
		: DatabaseScenarioBase
	{
		private readonly Guid userId = Guid.NewGuid();

		protected override void Run(IDbCommand dbCmd)
		{
			if (this.IsFirstRun)
			{
				dbCmd.CreateTables(true,
					typeof(Employees),
					typeof(Categories),
					typeof(Customers),
					typeof(Shippers),
					typeof(Suppliers),
					typeof(Orders),
					typeof(Products),
					typeof(OrderDetails),
					typeof(CustomerCustomerDemo),
					typeof(Categories),
					typeof(CustomerDemographics),
					typeof(Region),
					typeof(Territories),
					typeof(EmployeeTerritories)
				);
			}

			dbCmd.Insert(SampleOrderLine.Create(userId, this.Iteration, 1));
		}
	}

}