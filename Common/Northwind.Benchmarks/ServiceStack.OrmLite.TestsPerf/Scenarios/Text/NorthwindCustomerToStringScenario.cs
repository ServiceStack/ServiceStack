using Northwind.Common.ServiceModel;
using Northwind.Perf;
using ServiceStack.Common.Text;

namespace ServiceStack.OrmLite.TestsPerf.Scenarios.Text
{
	public class NorthwindCustomerToStringScenario
		: ScenarioBase
	{
		private readonly CustomerDto customer = NorthwindDtoFactory.Customer(
			1.ToString("x"), "Alfreds Futterkiste", "Maria Anders", "Sales Representative", "Obere Str. 57", 
			"Berlin", null, "12209", "Germany", "030-0074321", "030-0076545", null);

		public override void Run()
		{
			ToStringMethods.ToString(customer);
		}
	}
}