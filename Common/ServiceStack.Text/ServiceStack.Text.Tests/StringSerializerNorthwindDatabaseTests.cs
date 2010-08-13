using System;
using System.Collections.Generic;
using Northwind.Common.DataModel;
using NUnit.Framework;
using ServiceStack.Common.Tests;

namespace ServiceStack.Text.Tests
{
	[TestFixture]
	public class StringSerializerNorthwindDatabaseTests
		: TestBase
	{
		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			NorthwindData.LoadData(false);
		}


		[Test]
		public void serialize_Categories()
		{
			Serialize(NorthwindData.Categories);
		}

		[Test]
		public void serialize_Customers()
		{
			Serialize(NorthwindData.Customers);
		}

		[Test]
		public void serialize_Employees()
		{
			Serialize(NorthwindData.Employees);
		}

		[Test]
		public void serialize_EmployeeTerritories()
		{
			Serialize(NorthwindData.EmployeeTerritories);
		}

		[Test]
		public void serialize_OrderDetails()
		{
			Serialize(NorthwindData.OrderDetails);
		}

		[Test]
		public void serialize_Orders()
		{
			Serialize(NorthwindData.Orders);
		}

		[Test]
		public void serialize_Products()
		{
			Serialize(NorthwindData.Products);
		}

		[Test]
		public void serialize_Regions()
		{
			Serialize(NorthwindData.Regions);
		}

		[Test]
		public void serialize_Shippers()
		{
			Serialize(NorthwindData.Shippers);
		}

		[Test]
		public void serialize_Suppliers()
		{
			Serialize(NorthwindData.Suppliers);
		}

		[Test]
		public void serialize_Territories()
		{
			Serialize(NorthwindData.Territories);
		}

	}
}