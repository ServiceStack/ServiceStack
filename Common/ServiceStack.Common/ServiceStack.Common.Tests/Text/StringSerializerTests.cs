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

			var dtoString = StringSerializer.SerializeToString(dto);
			dtoString = StringSerializer.SerializeToString(dto);

			Assert.That(dtoString, Is.Not.Null);
		}

		[Test]
		public void Can_convert_to_CustomerOrderListDto()
		{
			var dto = DtoFactory.CustomerOrderListDto;

			var dtoString = StringSerializer.SerializeToString(dto);
			Console.WriteLine(dtoString);
			var fromDto = StringSerializer.DeserializeFromString<CustomerOrderListDto>(dtoString);

			Assert.That(fromDto, Is.Not.Null);
		}

		[Test]
		public void Can_convert_to_Customers()
		{
			var dto = NorthwindData.Customers;

			var dtoString = StringSerializer.SerializeToString(dto);
			Console.WriteLine(dtoString);
			var fromDto = StringSerializer.DeserializeFromString<List<Customer>>(dtoString);

			Assert.That(fromDto, Is.Not.Null);
		}

		[Test]
		public void Can_convert_to_Orders()
		{
			NorthwindData.LoadData(false);
			var dto = NorthwindData.Orders;

			var dtoString = StringSerializer.SerializeToString(dto);
			Console.WriteLine(dtoString);
			var fromDto = StringSerializer.DeserializeFromString<List<Order>>(dtoString);

			Assert.That(fromDto, Is.Not.Null);
		}

	}
}