using System;
using System.Collections.Generic;
using Northwind.Common.ComplexModel;
using Northwind.Common.DataModel;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Common.Text;

namespace ServiceStack.Common.Tests.Text
{
	[TestFixture]
	public class StringSerializerTests
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

			var dtoString = TypeSerializer.SerializeToString(dto);
			dtoString = TypeSerializer.SerializeToString(dto);

			Assert.That(dtoString, Is.Not.Null);
		}

		[Test]
		public void Can_convert_to_CustomerOrderListDto()
		{
			var dto = DtoFactory.CustomerOrderListDto;

			var dtoString = TypeSerializer.SerializeToString(dto);
			Console.WriteLine(dtoString);
			var fromDto = TypeSerializer.DeserializeFromString<CustomerOrderListDto>(dtoString);

			Assert.That(fromDto, Is.Not.Null);
		}

		[Test]
		public void Can_convert_to_Customers()
		{
			var dto = NorthwindData.Customers;

			var dtoString = TypeSerializer.SerializeToString(dto);
			Console.WriteLine(dtoString);
			var fromDto = TypeSerializer.DeserializeFromString<List<Customer>>(dtoString);

			Assert.That(fromDto, Is.Not.Null);
		}

		[Test]
		public void Can_convert_to_Orders()
		{
			NorthwindData.LoadData(false);
			var dto = NorthwindData.Orders;

			var dtoString = TypeSerializer.SerializeToString(dto);
			Console.WriteLine(dtoString);
			var fromDto = TypeSerializer.DeserializeFromString<List<Order>>(dtoString);

			Assert.That(fromDto, Is.Not.Null);
		}

	}
}