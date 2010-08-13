#if !MONO
using System;
using System.Collections.Generic;
using Northwind.Common.ComplexModel;
using Northwind.Common.DataModel;
using NUnit.Framework;

namespace ServiceStack.Text.Tests
{
	[TestFixture]
	public class StringSerializerTests
		: TestBase
	{
		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			NorthwindData.LoadData(false);
		}

		[Test]
		public void Can_convert_CustomerOrderListDto()
		{
			var dto = DtoFactory.CustomerOrderListDto;

			Serialize(dto);
		}

		[Test]
		public void Can_convert_to_CustomerOrderListDto()
		{
			var dto = DtoFactory.CustomerOrderListDto;

			Serialize(dto);
		}

		[Test]
		public void Can_convert_to_Customers()
		{
			var dto = NorthwindData.Customers;

			Serialize(dto);
		}

		[Test]
		public void Can_convert_to_Orders()
		{
			NorthwindData.LoadData(false);
			var dto = NorthwindData.Orders;

			Serialize(dto);
		}

	}
}

#endif
