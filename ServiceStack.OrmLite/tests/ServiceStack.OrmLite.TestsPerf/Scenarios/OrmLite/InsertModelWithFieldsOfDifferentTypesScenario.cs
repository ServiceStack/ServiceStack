using System.Data;
using Northwind.Perf;
using ServiceStack.OrmLite.TestsPerf.Model;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.TestsPerf.Scenarios.OrmLite
{
	public class InsertModelWithFieldsOfDifferentTypesPerfScenario
		: DatabaseScenarioBase
	{
		protected override void Run(IDbConnection db)
		{
			if (this.IsFirstRun)
			{
				db.CreateTable<ModelWithFieldsOfDifferentTypesPerf>(true);
			}

			db.Insert(ModelWithFieldsOfDifferentTypesPerf.Create(this.Iteration));
		}
	}

	public class SelectOneModelWithFieldsOfDifferentTypesPerfScenario
		: DatabaseScenarioBase
	{
        protected override void Run(IDbConnection db)
		{
			if (this.IsFirstRun)
			{
				db.CreateTable<ModelWithFieldsOfDifferentTypesPerf>(true);
				db.Insert(ModelWithFieldsOfDifferentTypesPerf.Create(this.Iteration));
			}

			var row = db.Select<ModelWithFieldsOfDifferentTypesPerf>();
		}
	}

	public class SelectManyModelWithFieldsOfDifferentTypesPerfScenario
		: DatabaseScenarioBase
	{
        protected override void Run(IDbConnection db)
		{
			if (this.IsFirstRun)
			{
				db.CreateTable<ModelWithFieldsOfDifferentTypesPerf>(true);
				20.Times(i => db.Insert(ModelWithFieldsOfDifferentTypesPerf.Create(i)));
			}

			var rows = db.Select<ModelWithFieldsOfDifferentTypesPerf>();
		}
	}

}