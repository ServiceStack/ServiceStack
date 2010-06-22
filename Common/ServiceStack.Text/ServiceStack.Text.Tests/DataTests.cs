using System;
using System.Collections.Generic;
using Northwind.Common.DataModel;
using NUnit.Framework;

namespace ServiceStack.Text.Tests
{
	[TestFixture]
	public class DataStressTests
	{
		public class TestClass
		{
			public string Value { get; set; }

			public bool Equals(TestClass other)
			{
				if (ReferenceEquals(null, other)) return false;
				if (ReferenceEquals(this, other)) return true;
				return Equals(other.Value, Value);
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != typeof (TestClass)) return false;
				return Equals((TestClass) obj);
			}

			public override int GetHashCode()
			{
				return (Value != null ? Value.GetHashCode() : 0);
			}
		}

		public T Serialize<T>(T model)
		{
			var strModel = TypeSerializer.SerializeToString(model);
			Console.WriteLine("Len: " + strModel.Length + ", " + strModel);
			var toModel = TypeSerializer.DeserializeFromString<T>(strModel);
			Assert.That(model.Equals(toModel));
			return toModel;
		}


		[Test]
		public void serialize_Customer_BOLID()
		{
			var customer = NorthwindFactory.Customer(
				"BOLID", "Bólido Comidas preparadas", "Martín Sommer", "Owner", "C/ Araquil, 67",
				"Madrid", null, "28023", "Spain", "(91) 555 22 82", "(91) 555 91 99", null);

			var model = new TestClass
      		{
      			Value = TypeSerializer.SerializeToString(customer)
      		};

			var toModel = Serialize(model);
			Console.WriteLine("toModel.Value: " + toModel.Value);

			var toCustomer = TypeSerializer.DeserializeFromString<Customer>(toModel.Value);
			Console.WriteLine("customer.Address: " + customer.Address);
			Console.WriteLine("toCustomer.Address: " + toCustomer.Address);
		}
		
	}
}