using System;
using Northwind.Common.ServiceModel;
using Northwind.Perf;
using ServiceStack.Common.Text;

namespace ServiceStack.OrmLite.TestsPerf.Scenarios.Text
{
	public class NorthwindOrderToStringScenario
		: ScenarioBase
	{
		readonly OrderDto order = NorthwindDtoFactory.Order(
			1, "VINET", 5, new DateTime(1996, 7, 4), new DateTime(1996, 1, 8), new DateTime(1996, 7, 16), 
			3, 32.38m, "Vins et alcools Chevalier", "59 rue de l'Abbaye", "Reims", null, "51100", "France");

		public override void Run()
		{
			ToStringMethods.ToString(order);
		}
	}
}