using Northwind.Benchmarks.Serialization;

namespace Nortwind.Benchmarks.Console
{
	class Program
	{
		static void Main(string[] args)
		{
			var noOfIterations = 1;
			int.TryParse(args[0], out noOfIterations);

			RunNorthwindSerializationBenchmarks(noOfIterations);
		}

		private static void RunNorthwindSerializationBenchmarks(int iterations)
		{
			var fixture = new NorthwindDatabaseSerializtionPerf(iterations);

			fixture.serialize_Categories();
			fixture.serialize_Customers();
			fixture.serialize_Employees();
			fixture.serialize_EmployeeTerritories();
			fixture.serialize_OrderDetails();
			fixture.serialize_Orders();
			fixture.serialize_Products();
			fixture.serialize_Regions();
			fixture.serialize_Shippers();
			fixture.serialize_Suppliers();
			fixture.serialize_Territories();

			fixture.GenerateHtmlReport();
		}
	}
}
