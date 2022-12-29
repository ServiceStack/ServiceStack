using System;
using System.Data;
using Northwind.Perf;
using ServiceStack.OrmLite.TestsPerf.Model;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.TestsPerf.Scenarios.OrmLite
{
	public class InsertSampleOrderLineScenario
		: DatabaseScenarioBase
	{
		private readonly Guid userId = Guid.NewGuid();

        protected override void Run(IDbConnection db)
		{
			if (this.IsFirstRun)
			{
				db.CreateTable<SampleOrderLine>(true);
			}

			db.Insert(SampleOrderLine.Create(userId, this.Iteration, 1));
		}
	}

	public class SelectOneSampleOrderLineScenario
		: DatabaseScenarioBase
	{
		private readonly Guid userId = Guid.NewGuid();

        protected override void Run(IDbConnection db)
		{
			if (this.IsFirstRun)
			{
				db.CreateTable<SampleOrderLine>(true);
				db.Insert(SampleOrderLine.Create(userId, this.Iteration, 1));
			}

			var row = db.Select<SampleOrderLine>();
		}
	}

	public class SelectManySampleOrderLineScenario
		: DatabaseScenarioBase
	{
		private readonly Guid userId = Guid.NewGuid();

        protected override void Run(IDbConnection db)
		{
			if (this.IsFirstRun)
			{
				db.CreateTable<SampleOrderLine>(true);
				20.Times(i => db.Insert(SampleOrderLine.Create(userId, i, 1)));
			}

			var rows = db.Select<SampleOrderLine>();
		}
	}
}