using System.Collections.Generic;
using Northwind.Common.DataModel;
using NUnit.Framework;

namespace ServiceStack.OrmLite.TestsPerf.Tests
{
	[TestFixture]
	public class NorthwindDatabaseSerializtionPerf
		: SerializationTestBase
	{
		public NorthwindDatabaseSerializtionPerf()
		{
			this.MultipleIterations = new List<int> { 1, 10 };

			NorthwindDtoData.LoadData(false);
		}


		[Test]
		public void serialize_Categories()
		{
			SerializeDto(NorthwindDtoData.Categories);
		}

		[Test]
		public void serialize_Customers()
		{
			SerializeDto(NorthwindDtoData.Customers);
		}

		[Test]
		public void serialize_Employees()
		{
			SerializeDto(NorthwindDtoData.Employees);
		}

		[Test]
		public void serialize_EmployeeTerritories()
		{
			SerializeDto(NorthwindDtoData.EmployeeTerritories);
		}

		[Test]
		public void serialize_OrderDetails()
		{
			SerializeDto(NorthwindDtoData.OrderDetails);
		}

		[Test]
		public void serialize_Orders()
		{
			SerializeDto(NorthwindDtoData.Orders);
		}

		[Test]
		public void serialize_Products()
		{
			SerializeDto(NorthwindDtoData.Products);
		}

		[Test]
		public void serialize_Regions()
		{
			SerializeDto(NorthwindDtoData.Regions);
		}

		[Test]
		public void serialize_Shippers()
		{
			SerializeDto(NorthwindDtoData.Shippers);
		}

		[Test]
		public void serialize_Suppliers()
		{
			SerializeDto(NorthwindDtoData.Suppliers);
		}

		[Test]
		public void serialize_Territories()
		{
			SerializeDto(NorthwindDtoData.Territories);
		}

	}
}