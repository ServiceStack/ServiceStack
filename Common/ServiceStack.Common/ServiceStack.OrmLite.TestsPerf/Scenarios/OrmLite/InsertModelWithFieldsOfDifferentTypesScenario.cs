using System.Data;
using ServiceStack.Common.Extensions;
using ServiceStack.OrmLite.Tests.Models;

namespace ServiceStack.OrmLite.TestsPerf.Scenarios.OrmLite
{
	public class InsertModelWithFieldsOfDifferentTypesScenario
		: DatabaseScenarioBase
	{
		protected override void Run(IDbCommand dbCmd)
		{
			if (this.IsFirstRun)
			{
				dbCmd.CreateTable<ModelWithFieldsOfDifferentTypes>(true);
			}

			dbCmd.Insert(ModelWithFieldsOfDifferentTypes.Create(this.Iteration));
		}
	}

	public class SelectOneModelWithFieldsOfDifferentTypesScenario
		: DatabaseScenarioBase
	{
		protected override void Run(IDbCommand dbCmd)
		{
			if (this.IsFirstRun)
			{
				dbCmd.CreateTable<ModelWithFieldsOfDifferentTypes>(true);
				dbCmd.Insert(ModelWithFieldsOfDifferentTypes.Create(this.Iteration));
			}

			var row = dbCmd.Select<ModelWithFieldsOfDifferentTypes>();
		}
	}

	public class SelectManyModelWithFieldsOfDifferentTypesScenario
		: DatabaseScenarioBase
	{
		protected override void Run(IDbCommand dbCmd)
		{
			if (this.IsFirstRun)
			{
				dbCmd.CreateTable<ModelWithFieldsOfDifferentTypes>(true);
				20.Times(i => dbCmd.Insert(ModelWithFieldsOfDifferentTypes.Create(i)));
			}

			var rows = dbCmd.Select<ModelWithFieldsOfDifferentTypes>();
		}
	}

}