using Northwind.Common.DataModel;
using Northwind.Common.ComplexModel;
using NUnit.Framework;
using ServiceStack.Serialization;
using ServiceStack.Text;

namespace ServiceStack.ServiceModel.Tests
{
	[TestFixture]
	public class ServiceModelSerializerTests
	{
		[TestFixtureSetUp]
		public void OnTestFixtureSetUp()
		{
			NorthwindData.LoadData(false);			
		}

		[Test]
		public void Can_serialize_northind_models()
		{
			Serialize(NorthwindData.Categories);
			Serialize(NorthwindData.Customers);
			Serialize(NorthwindData.Employees);
			Serialize(NorthwindData.Shippers);
			Serialize(NorthwindData.Orders);
			Serialize(NorthwindData.Products);
			Serialize(NorthwindData.OrderDetails);
			Serialize(NorthwindData.CustomerCustomerDemos);
			Serialize(NorthwindData.Regions);
			Serialize(NorthwindData.Territories);
			Serialize(NorthwindData.EmployeeTerritories);
		}

		[Test]
		[Ignore("Could not find Platform.dll")]
		public void Can_serialize_complex_northind_dtos()
		{
			Serialize(DtoFactory.ArrayDtoWithOrders);
			Serialize(DtoFactory.CustomerDto);
			Serialize(DtoFactory.CustomerOrderArrayDto);
			Serialize(DtoFactory.CustomerOrderListDto);
			Serialize(DtoFactory.MultiCustomerProperties);
			Serialize(DtoFactory.MultiDtoWithOrders);
			Serialize(DtoFactory.MultiOrderProperties);
			Serialize(DtoFactory.OrderDto);
			Serialize(DtoFactory.SupplierDto);
		}

		public void Serialize<T>(T model)
		{
			var serializedXml = DataContractSerializer.Instance.Parse(model);
			var deserializedXml = DataContractDeserializer.Instance.Parse(serializedXml, typeof(T));
			Assert.That(deserializedXml, Is.Not.Null, "XML serialization error for: " + typeof(T).Name);

			var serializedJson = JsonDataContractSerializer.Instance.SerializeToString(model);
			var deserializedJson = JsonDataContractDeserializer.Instance.DeserializeFromString(serializedJson, typeof(T));
			Assert.That(deserializedJson, Is.Not.Null, "JSON serialization error for: " + typeof(T).Name);

			var serializedJsv = TypeSerializer.SerializeToString(model);
			var deserializedJsv = TypeSerializer.DeserializeFromString<T>(serializedJsv);
			Assert.That(deserializedJsv, Is.Not.Null, "JSV serialization error for: " + typeof(T).Name);
		}
	}
}