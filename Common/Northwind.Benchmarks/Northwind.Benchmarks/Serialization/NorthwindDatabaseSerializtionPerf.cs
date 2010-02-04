using System;
using System.Collections.Generic;
using System.IO;
using Northwind.Common.DataModel;
using NUnit.Framework;
using ServiceStack.Common.Utils;

namespace Northwind.Benchmarks.Serialization
{
	[TestFixture]
	public class NorthwindDatabaseSerializtionPerf
		: SerializationTestBase
	{
		public NorthwindDatabaseSerializtionPerf()
		{
			this.MultipleIterations = new List<int> { 10 };

			NorthwindDtoData.LoadData(false);
		}

		[TestFixtureTearDown]
		public void AfterAllTests()
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
			SerializeDto(NorthwindDtoData.Instance);
		}

	}
}