using Northwind.Common.ServiceModel;
using Northwind.Perf;
using ServiceStack.Common.Text;

namespace ServiceStack.OrmLite.TestsPerf.Scenarios.Text
{
	public class NorthwindSupplierToStringScenario
		: ScenarioBase
	{
		private readonly SupplierDto supplier = NorthwindDtoFactory.Supplier(
			1, "Exotic Liquids", "Charlotte Cooper", "Purchasing Manager", "49 Gilbert St.", "London", null, 
			"EC1 4SD", "UK", "(171) 555-2222", null, null);

		public override void Run()
		{
			StringSerializer.SerializeToString(supplier);
		}
	}

}