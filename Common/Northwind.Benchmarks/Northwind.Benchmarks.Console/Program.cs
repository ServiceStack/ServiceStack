using Northwind.Benchmarks.Serialization;

namespace Northwind.Benchmarks.Console
{
	class Program
	{
		static void Main(string[] args)
		{
			int noOfIterations;
			if (args.Length < 1 || !int.TryParse(args[0], out noOfIterations))
				noOfIterations = 1;

			//Run_NorthwindDatabaseRowsSerialization(noOfIterations);
			Run_NorthwindDatabaseTablesSerialization(noOfIterations);
		}

		private static void Run_NorthwindDatabaseRowsSerialization(int iterations)
		{
			var fixture = new NorthwindDatabaseRowsSerialization(iterations);

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

			fixture.AfterAllTests();
		}

		private static void Run_NorthwindDatabaseTablesSerialization(int iterations)
		{
			var fixture = new NorthwindDatabaseTablesSerialization(iterations);

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

			fixture.AfterAllTests();
		}
	}
}