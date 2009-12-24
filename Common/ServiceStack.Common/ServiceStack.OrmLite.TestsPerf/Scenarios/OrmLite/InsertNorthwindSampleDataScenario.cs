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

			dbCmd.Insert(SampleOrderLine.Create(userId, this.Iteration, 1));
		}
	}

}