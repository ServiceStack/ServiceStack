using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Northwind.Common.DataModel;
using NUnit.Framework;
using ServiceStack.Common.Utils;

namespace Northwind.Benchmarks.Serialization
{
	[TestFixture]
	public class NorthwindDatabaseTablesSerializtion
		: SerializationTestBase
	{
		public NorthwindDatabaseTablesSerializtion(int iterations)
			: this()
		{
			this.MultipleIterations = new List<int> { iterations };
		}

		public NorthwindDatabaseTablesSerializtion()
		{
			this.MultipleIterations = new List<int> { 100 };

			NorthwindDtoData.LoadData(false);

			this.HtmlSummary =
				"These benchmarks show the total time in ticks (1/1000ms) that it takes"
			  + " each serializer to serialize and deserialize each entire table from the "
			  + " <a href='http://code.google.com/p/servicestack/source/browse/trunk/Common/Northwind.Benchmarks/Northwind.Common/DataModel/NorthwindData.cs'>Northwind Database</a>"
			  + " (3202 records) " + this.MultipleIterations.Sum() + " Times. <br/><br/>"
			  + "The full source code of the serialization benchmarks (which generated this report)"
			  + " is <a href='http://code.google.com/p/servicestack/source/browse/trunk/Common/Northwind.Benchmarks/Nortwind.Benchmarks.Console/Program.cs'>available here</a>.";
		}

		[TestFixtureTearDown]
		public void GenerateHtmlReport()
		{
			var path =
				"~/_Results/Serialization/".MapAbsolutePath()
				+ string.Format("Northwind-database.{0:yyyy-MM-dd}.html", DateTime.Now);

			File.WriteAllText(path, base.ToHtmlReport());
		}

		[Test]
		public void serialize_Categories()
		{
			SerializeDto(NorthwindDtoData.Instance.Categories);
		}

		[Test]
		public void serialize_Customers()
		{
			SerializeDto(NorthwindDtoData.Instance.Customers);
		}

		[Test]
		public void serialize_Employees()
		{
			SerializeDto(NorthwindDtoData.Instance.Employees);
		}

		[Test]
		public void serialize_EmployeeTerritories()
		{
			SerializeDto(NorthwindDtoData.Instance.EmployeeTerritories);
		}

		[Test]
		public void serialize_OrderDetails()
		{
			SerializeDto(NorthwindDtoData.Instance.OrderDetails);
		}

		[Test]
		public void serialize_Orders()
		{
			SerializeDto(NorthwindDtoData.Instance.Orders);
		}

		[Test]
		public void serialize_Products()
		{
			SerializeDto(NorthwindDtoData.Instance.Products);
		}

		[Test]
		public void serialize_Regions()
		{
			SerializeDto(NorthwindDtoData.Instance.Regions);
		}

		[Test]
		public void serialize_Shippers()
		{
			SerializeDto(NorthwindDtoData.Instance.Shippers);
		}

		[Test]
		public void serialize_Suppliers()
		{
			SerializeDto(NorthwindDtoData.Instance.Suppliers);
		}

		[Test]
		public void serialize_Territories()
		{
			SerializeDto(NorthwindDtoData.Instance.Territories);
		}

		[Test]
		public void serialize_EntireDatabase()
		{
			//			SerializeDto(NorthwindDtoData.Instance);
		}

	}
}