using System;
using System.Data;
using Northwind.Perf;
using ServiceStack.Common.Extensions;
using ServiceStack.OrmLite.TestsPerf.Model;

namespace ServiceStack.OrmLite.TestsPerf.Scenarios.OrmLite
{
	public class InsertSampleOrderLineScenario
		: DatabaseScenarioBase
	{
		private readonly Guid userId = Guid.NewGuid();

		protected override void Run(IDbCommand dbCmd)
		{
			if (this.IsFirstRun)
			{
				dbCmd.CreateTable<SampleOrderLine>(true);
			}

			dbCmd.Insert(SampleOrderLine.Create(userId, this.Iteration, 1));
		}
	}

	public class SelectOneSampleOrderLineScenario
		: DatabaseScenarioBase
	{
		private readonly Guid userId = Guid.NewGuid();

		protected override void Run(IDbCommand dbCmd)
		{
			if (this.IsFirstRun)
			{
				dbCmd.CreateTable<SampleOrderLine>(true);
				dbCmd.Insert(SampleOrderLine.Create(userId, this.Iteration, 1));
			}

			var row = dbCmd.Select<SampleOrderLine>();
		}
	}

	public class SelectManySampleOrderLineScenario
		: DatabaseScenarioBase
	{
		private readonly Guid userId = Guid.NewGuid();

		protected override void Run(IDbCommand dbCmd)
		{
			if (this.IsFirstRun)
			{
				dbCmd.CreateTable<SampleOrderLine>(true);
				20.Times(i => dbCmd.Insert(SampleOrderLine.Create(userId, i, 1)));
			}

			var rows = dbCmd.Select<SampleOrderLine>();
		}
	}
}