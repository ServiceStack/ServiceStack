using System;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Common.Tests.Perf;
using ServiceStack.Common.Text;
using ServiceStack.Common.Utils;
using ServiceStack.OrmLite.Tests.Models;

namespace ServiceStack.OrmLite.TestsPerf.Tests
{
	[TestFixture]
	public class TypeToStringTests
		: TextTestBase
	{
		[Test]
		public void Can_deserialize_CustomerDto()
		{
			var dto = NorthwindDtoFactory.Customer(
			1.ToString("x"), "Alfreds Futterkiste", "Maria Anders", "Sales Representative", "Obere Str. 57",
			"Berlin", null, "12209", "Germany", "030-0074321", "030-0076545", null);

			var dtoString = StringConverterUtils.ToString(dto);

			Log(dtoString);

			var newDto = StringConverterUtils.Parse<CustomerDto>(dtoString);

			Assert.That(dto.Equals(newDto), Is.True);
		}

		[Test]
		public void Can_deserialize_OrderDto()
		{
			var dto = NorthwindDtoFactory.Order(
				1, "VINET", 5, new DateTime(1996, 7, 4), new DateTime(1996, 1, 8), new DateTime(1996, 7, 16),
				3, 32.38m, "Vins et alcools Chevalier", "59 rue de l'Abbaye", "Reims", null, "51100", "France");

			var dtoString = StringConverterUtils.ToString(dto);

			Log(dtoString);

			var newDto = StringConverterUtils.Parse<OrderDto>(dtoString);

			Assert.That(dto.Equals(newDto), Is.True);
		}

		[Test]
		public void Can_deserialize_SupplierDto()
		{
			var dto = NorthwindDtoFactory.Supplier(
				1, "Exotic Liquids", "Charlotte Cooper", "Purchasing Manager", "49 Gilbert St.", "London", null,
				"EC1 4SD", "UK", "(171) 555-2222", null, null);

			var dtoString = StringConverterUtils.ToString(dto);

			Log(dtoString);

			var newDto = StringConverterUtils.Parse<SupplierDto>(dtoString);

			Assert.That(dto.Equals(newDto), Is.True);
		}
	}
}