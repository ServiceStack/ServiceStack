using Northwind.Benchmarks.Serialization;

namespace Northwind.Benchmarks.Console
{
	class Program
	{
		static void Main(string[] args)
		{
			System.Console.WriteLine(string.Join(", ", args));

			int noOfIterations;
			if (args.Length < 1 || !int.TryParse(args[0], out noOfIterations))
				noOfIterations = 1;

			RunNorthwindSerializationBenchmarks(noOfIterations);
		}

		private static void RunNorthwindSerializationBenchmarks(int iterations)
		{
			var fixture = new NorthwindDatabaseTablesSerializtion(iterations);

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