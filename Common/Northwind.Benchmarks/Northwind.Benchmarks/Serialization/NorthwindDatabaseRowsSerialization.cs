using System.Collections.Generic;
using System.Linq;
using Northwind.Common.DataModel;
using NUnit.Framework;

namespace Northwind.Benchmarks.Serialization
{
	[TestFixture]
	public class NorthwindDatabaseRowsSerialization
		: SerializationTestBase
	{
		public NorthwindDatabaseRowsSerialization(int iterations)
			: this()
		{
			this.MultipleIterations = new List<int> { iterations };
		}

		public NorthwindDatabaseRowsSerialization()
		{
			this.MultipleIterations = new List<int> { 10000 };

			NorthwindDtoData.LoadData(false);
		}

		[TestFixtureTearDown]
		public void AfterAllTests()
		{
			var htmlSummary =
				"These benchmarks show the total time in ticks (1/1000ms) that it takes"
			  + " each serializer to serialize and deserialize the first row from each table in the "
			  + " <a href='http://code.google.com/p/servicestack/source/browse/trunk/Common/Northwind.Benchmarks/Northwind.Common/DataModel/NorthwindData.cs'>Northwind Database</a>"
			  + "<strong>" + this.MultipleIterations.Sum().ToString("#,##0") + "</strong> Times. <br/><br/>"
			  + "The full source code of the serialization benchmarks (which generated this report)"
			  + " is <a href='http://code.google.com/p/servicestack/source/browse/trunk/Common/Northwind.Benchmarks/Northwind.Benchmarks.Console/Program.cs'>available here</a>.";

			base.GenerateHtmlReport(htmlSummary);
		}

		[Test]
		public void serialize_Categories()
		{
			SerializeDto(NorthwindDtoData.Instance.Categories[0]);
		}

		[Test]
		public void serialize_Customers()
		{
			SerializeDto(NorthwindDtoData.Instance.Customers[0]);
		}

		[Test]
		public void serialize_Employees()
		{
			SerializeDto(NorthwindDtoData.Instance.Employees[0]);
		}

		[Test]
		public void serialize_EmployeeTerritories()
		{
			SerializeDto(NorthwindDtoData.Instance.EmployeeTerritories[0]);
		}

		[Test]
		public void serialize_OrderDetails()
		{
			SerializeDto(NorthwindDtoData.Instance.OrderDetails[0]);
		}

		[Test]
		public void serialize_Orders()
		{
			SerializeDto(NorthwindDtoData.Instance.Orders[0]);
		}

		[Test]
		public void serialize_Products()
		{
			SerializeDto(NorthwindDtoData.Instance.Products[0]);
		}

		[Test]
		public void serialize_Regions()
		{
			SerializeDto(NorthwindDtoData.Instance.Regions[0]);
		}

		[Test]
		public void serialize_Shippers()
		{
			SerializeDto(NorthwindDtoData.Instance.Shippers[0]);
		}

		[Test]
		public void serialize_Suppliers()
		{
			SerializeDto(NorthwindDtoData.Instance.Suppliers[0]);
		}

		[Test]
		public void serialize_Territories()
		{
			SerializeDto(NorthwindDtoData.Instance.Territories[0]);
		}

	}
}