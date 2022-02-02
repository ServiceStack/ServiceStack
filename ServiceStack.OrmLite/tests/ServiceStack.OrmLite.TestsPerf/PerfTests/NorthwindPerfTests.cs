using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.OrmLite.TestsPerf.Scenarios.Northwind;

namespace ServiceStack.OrmLite.TestsPerf.PerfTests
{
	[Ignore("")]
	[TestFixture]
	public class NorthwindPerfTests
		: OrmLitePerfTests
	{
		public NorthwindPerfTests()
		{
			//this.MultipleIterations = new List<int> { 1000, 10000, 100000 };
			this.MultipleIterations = new List<int> { 10 };
		}

		[TestFixtureTearDown]
		public void TestFixtureTearDown()
		{
			WriteLog();
		}

		[Test]
		public void Run_InsertNorthwindOrderScenario()
		{
			RunMultipleTimes(new InsertNorthwindOrderScenario());
		}

		[Test]
		public void Run_InsertNorthwindCustomerScenario()
		{
			RunMultipleTimes(new InsertNorthwindCustomerScenario());
		}

		[Test]
		public void Run_InsertNorthwindSupplierScenario()
		{
			RunMultipleTimes(new InsertNorthwindSupplierScenario());
		}

		[Test]
		public void Run_SelectNorthwindSupplierScenario()
		{
			RunMultipleTimes(new SelectNorthwindSupplierScenario());
		}

		[Test]
		public void Run_InsertNorthwindDataScenario()
		{
			RunMultipleTimes(new InsertNorthwindDataScenario());
		}

		[Test]
		public void Run_SelectNorthwindDataScenario()
		{
			RunMultipleTimes(new SelectNorthwindDataScenario());
		}

	}

}