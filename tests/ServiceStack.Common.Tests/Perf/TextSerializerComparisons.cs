using System.Collections.Generic;
using Northwind.Common.ComplexModel;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests.Perf
{
	[Ignore("Benchmarks on the war of the two text serializers")]
	[TestFixture]
	public class TextSerializerComparisons
		: PerfTestBase
	{
		public TextSerializerComparisons()
		{
			this.MultipleIterations = new List<int> { 10000 };
		}

		private void CompareSerializers<T>(T dto)
		{
			CompareMultipleRuns(
				"TypeSerializer", () => TypeSerializer.SerializeToString(dto),
				"TextSerializer", () => JsonSerializer.SerializeToString(dto)
				);

			var stringStr = TypeSerializer.SerializeToString(dto);
			var textStr = JsonSerializer.SerializeToString(dto);

			//return;

			CompareMultipleRuns(
				"TypeSerializer", () => TypeSerializer.DeserializeFromString<T>(stringStr),
				"JsonSerializer", () => JsonSerializer.DeserializeFromString<T>(textStr)
			);

			var seraializedStringDto = TypeSerializer.DeserializeFromString<T>(stringStr);
			Assert.That(seraializedStringDto.Equals(dto), Is.True);

			JsonSerializer.DeserializeFromString<T>(textStr);
			//Assert.That(seraializedTextDto.Equals(dto), Is.True);
		}

		[Test]
		public void Compare_ArrayDtoWithOrders()
		{
			CompareSerializers(DtoFactory.ArrayDtoWithOrders);
		}

		[Test]
		public void Compare_CustomerDto()
		{
			CompareSerializers(DtoFactory.CustomerDto);
		}

		[Test]
		public void Compare_CustomerOrderArrayDto()
		{
			CompareSerializers(DtoFactory.CustomerOrderArrayDto);
		}

		[Test]
		public void Compare_CustomerOrderListDto()
		{
			CompareSerializers(DtoFactory.CustomerOrderListDto);
		}

		[Test]
		public void Compare_MultiCustomerProperties()
		{
			CompareSerializers(DtoFactory.MultiCustomerProperties);
		}

		[Test]
		public void Compare_MultiDtoWithOrders()
		{
			CompareSerializers(DtoFactory.MultiDtoWithOrders);
		}

		[Test]
		public void Compare_MultiOrderProperties()
		{
			CompareSerializers(DtoFactory.MultiOrderProperties);
		}

		[Test]
		public void Compare_OrderDto()
		{
			CompareSerializers(DtoFactory.OrderDto);
		}

		[Test]
		public void Compare_SupplierDto()
		{
			CompareSerializers(DtoFactory.SupplierDto);
		}
	}

}