using System.Collections.Generic;
using Northwind.Common.ComplexModel;
using NUnit.Framework;

namespace Northwind.Benchmarks.Serialization
{
	[TestFixture]
	public class SingleModelSerializationPerf
		: SerializationTestBase
	{
		public SingleModelSerializationPerf()
		{
			this.MultipleIterations = new List<int> { 10000 };
		}

		private void DoAll<T>(T dto)
		{
			AssertAllAreEqual(dto);

			SerializeDto(dto);
		}

		[Test]
		public void serialize_Customer()
		{
			DoAll(DtoFactory.CustomerDto);
		}

		[Test]
		public void serialize_Customers()
		{
			DoAll(DtoFactory.CustomerDto);
		}

		[Test]
		public void serialize_Order()
		{
			DoAll(DtoFactory.OrderDto);
		}

		[Test]
		public void serialize_Supplier()
		{
			DoAll(DtoFactory.SupplierDto);
		}

		[Test]
		public void serialize_MultiOrderProperties()
		{
			DoAll(DtoFactory.MultiOrderProperties);
		}

		[Test]
		public void serialize_MultiCustomerProperties()
		{
			DoAll(DtoFactory.MultiCustomerProperties);
		}

		[Test]
		public void serialize_CustomerOrderArrayDto()
		{
			DoAll(DtoFactory.CustomerOrderArrayDto);
		}

		[Test]
		public void serialize_CustomerOrderListDto()
		{
			DoAll(DtoFactory.CustomerOrderListDto);
		}

		[Test]
		public void serialize_ArrayDtoWithOrders()
		{
			DoAll(DtoFactory.ArrayDtoWithOrders);
		}

	}
}