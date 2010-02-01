using System.Collections.Generic;
using System.IO;
using System.Text;
using Northwind.Common.ComplexModel;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using Platform.Text;
using ServiceStack.Common.Text;

namespace ServiceStack.Common.Tests.Perf
{
	[Ignore("Benchmarks on the war of the two text serializers")]
	[TestFixture]
	public class TypeToStringComparisons
		: PerfTestBase
	{
		public TypeToStringComparisons()
		{
			this.MultipleIterations = new List<int> { 10000 };
		}

		private void CompareSerializers<T>(T dto)
		{
			var typeToStringFn = TypeToStringMethods.GetToStringMethod(typeof(T));
			var jsvTypeToStringFn = Common.Text.Jsv.TypeToStringMethods<T>.GetToStringMethod();
			CompareMultipleRuns(
				"TypeSerializer", () =>
                  	{
						using (var writer = new StringWriter(new StringBuilder()))
							typeToStringFn(writer, dto);
                  	},
				"Jsv.TypeSerializer", () =>
                  	{
						using (var writer = new StringWriter(new StringBuilder()))
							jsvTypeToStringFn(writer, dto);
					}
				);

			return;

			var stringStr = TypeSerializer.SerializeToString(dto);
			var textStr = Common.Text.Jsv.TypeSerializer.SerializeToString(dto);

			CompareMultipleRuns(
				"TypeSerializer", () => TypeSerializer.DeserializeFromString<T>(stringStr),
				"Jsv.TypeSerializer", () => Common.Text.Jsv.TypeSerializer.DeserializeFromString<T>(textStr)
				);

			var seraializedStringDto = TypeSerializer.DeserializeFromString<T>(stringStr);
			Assert.That(seraializedStringDto.Equals(dto), Is.True);

			var seraializedTextDto = Common.Text.Jsv.TypeSerializer.DeserializeFromString<T>(textStr);
			Assert.That(seraializedTextDto.Equals(dto), Is.True);
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