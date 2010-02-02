using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Jayrock.Json.Conversion;
using Northwind.Common.ComplexModel;
using Northwind.Common.ServiceModel;
using Northwind.Perf;
using NUnit.Framework;
using Platform.Text;
using ProtoBuf;
using ServiceStack.Client;
using ServiceStack.Common.Text.Jsv;
using ServiceStack.Common.Utils;

namespace Northwind.Benchmarks.Serialization
{
	[TestFixture]
	public class TextSerializerPerfTests
		: PerfTestBase
	{
		public TextSerializerPerfTests()
		{
			this.MultipleIterations = new List<int> { 1000, 10000 };
		}

		public void WriteLog()
		{
			var fileName = string.Format("~/App_Data/PerfTests/TextSerializer.{0:yyyy-MM-dd}.log", DateTime.Now).MapAbsolutePath();
			using (var writer = new StreamWriter(fileName, true))
			{
				writer.Write(SbLog);
			}
		}

		protected void RunMultipleTimes(ScenarioBase scenarioBase)
		{
			RunMultipleTimes(scenarioBase.Run, scenarioBase.GetType().Name);
		}

		public string ProtoBufToString<T>(T dto)
		{
			return Encoding.UTF8.GetString(ProtoBufToBytes(dto));
		}

		public byte[] ProtoBufToBytes<T>(T dto)
		{
			using (var ms = new MemoryStream())
			{
				Serializer.Serialize(ms, dto);
				var bytes = ms.ToArray();
				return bytes;
			}
		}

		public T ProtoBufFromBytes<T>(byte[] bytes)
		{
			using (var ms = new MemoryStream(bytes))
			{
				return Serializer.Deserialize<T>(ms);
			}
		}

		[Test]
		public void Serialize_Customer()
		{
			var customer = NorthwindDtoFactory.Customer(
				1.ToString("x"), "Alfreds Futterkiste", "Maria Anders", "Sales Representative", "Obere Str. 57",
				"Berlin", null, "12209", "Germany", "030-0074321", "030-0076545", null);

			Log(TypeSerializer.SerializeToString(customer));
			Log(JsonConvert.ExportToString(customer));
			Log(ProtoBufToString(customer));

			RunMultipleTimes(() => DataContractSerializer.Instance.Parse(customer), "DataContractSerializer.Instance.Parse(customer)");
			RunMultipleTimes(() => JsonDataContractSerializer.Instance.Parse(customer), "JsonDataContractSerializer.Instance.Parse(customer)");
			RunMultipleTimes(() => JsonConvert.ExportToString(customer), "JsonConvert.ExportToString(customer)");
			RunMultipleTimes(() => ProtoBufToBytes(customer), "ProtoBufToBytes(customer)");
			RunMultipleTimes(() => TypeSerializer.SerializeToString(customer), "TypeSerializer.SerializeToString(customer)");
			RunMultipleTimes(() => TextSerializer.SerializeToString(customer), "TextSerializer.SerializeToString(customer)");
		}

		[Test]
		public void Run_NorthwindOrderToStringScenario()
		{
			var order = NorthwindDtoFactory.Order(
				1, "VINET", 5, new DateTime(1996, 7, 4), new DateTime(1996, 1, 8), new DateTime(1996, 7, 16), 
				3, 32.38m, "Vins et alcools Chevalier", "59 rue de l'Abbaye", "Reims", null, "51100", "France");

			Log(TypeSerializer.SerializeToString(order));
			Log(TextSerializer.SerializeToString(order));
			Log(JsonConvert.ExportToString(order));

			RunMultipleTimes(() => DataContractSerializer.Instance.Parse(order), "DataContractSerializer.Instance.Parse(order)");
			RunMultipleTimes(() => JsonDataContractSerializer.Instance.Parse(order), "JsonDataContractSerializer.Instance.Parse(order)");
			RunMultipleTimes(() => JsonConvert.ExportToString(order), "JsonConvert.ExportToString(order)");
			RunMultipleTimes(() => SerializationTestBase.ProtoBufToBytes(order), "SerializationTestBase.ProtoBufToBytes(order)");
			RunMultipleTimes(() => TypeSerializer.SerializeToString(order), "TypeSerializer.SerializeToString(order)");
			RunMultipleTimes(() => TextSerializer.SerializeToString(order), "TextSerializer.SerializeToString(order)");
		}

		[Test]
		public void Run_NorthwindSupplierToStringScenario()
		{
			var supplier = NorthwindDtoFactory.Supplier(
				1, "Exotic Liquids", "Charlotte Cooper", "Purchasing Manager", "49 Gilbert St.", "London", null,
				"EC1 4SD", "UK", "(171) 555-2222", null, null);

			Log(TypeSerializer.SerializeToString(supplier));
			Log(JsonConvert.ExportToString(supplier));

			RunMultipleTimes(() => DataContractSerializer.Instance.Parse(supplier), "DataContractSerializer.Instance.Parse(supplier)");
			RunMultipleTimes(() => JsonDataContractSerializer.Instance.Parse(supplier), "JsonDataContractSerializer.Instance.Parse(supplier)");
			RunMultipleTimes(() => JsonConvert.ExportToString(supplier), "JsonConvert.ExportToString(supplier)");
			RunMultipleTimes(() => TypeSerializer.SerializeToString(supplier), "TypeSerializer.SerializeToString(supplier)");
			RunMultipleTimes(() => TextSerializer.SerializeToString(supplier), "TextSerializer.SerializeToString(supplier)");
		}

		[Test]
		public void Serialize_MultiOrderProperties()
		{
			var dto = DtoFactory.MultiOrderProperties;

			Log(DataContractSerializer.Instance.Parse(dto));
			Log(JsonDataContractSerializer.Instance.Parse(dto));
			Log(JsonConvert.ExportToString(dto));
			Log(TypeSerializer.SerializeToString(dto));
			Log(TextSerializer.SerializeToString(dto));

			RunMultipleTimes(() => DataContractSerializer.Instance.Parse(dto), "DataContractSerializer.Instance.Parse(dto)");
			RunMultipleTimes(() => JsonDataContractSerializer.Instance.Parse(dto), "JsonDataContractSerializer.Instance.Parse(dto)");
			RunMultipleTimes(() => JsonConvert.ExportToString(dto), "JsonConvert.ExportToString(dto)");
			RunMultipleTimes(() => TypeSerializer.SerializeToString(dto), "TypeSerializer.SerializeToString(dto)");
			RunMultipleTimes(() => TextSerializer.SerializeToString(dto), "TextSerializer.SerializeToString(dto)");
		}

		[Test]
		public void Serialize_MultiCustomerProperties()
		{
			var dto = DtoFactory.MultiCustomerProperties;

			Log(DataContractSerializer.Instance.Parse(dto));
			Log(JsonDataContractSerializer.Instance.Parse(dto));
			Log(JsonConvert.ExportToString(dto));
			Log(TypeSerializer.SerializeToString(dto));
			Log(TextSerializer.SerializeToString(dto));

			RunMultipleTimes(() => DataContractSerializer.Instance.Parse(dto), "DataContractSerializer.Instance.Parse(dto)");
			RunMultipleTimes(() => JsonDataContractSerializer.Instance.Parse(dto), "JsonDataContractSerializer.Instance.Parse(dto)");
			RunMultipleTimes(() => JsonConvert.ExportToString(dto), "JsonConvert.ExportToString(dto)");
			RunMultipleTimes(() => TypeSerializer.SerializeToString(dto), "TypeSerializer.SerializeToString(dto)");
			RunMultipleTimes(() => TextSerializer.SerializeToString(dto), "TextSerializer.SerializeToString(dto)");
		}

		[Test]
		public void Serialize_MultiDtoWithOrders()
		{
			var dto = DtoFactory.MultiDtoWithOrders;

			Log(DataContractSerializer.Instance.Parse(dto));
			Log(JsonDataContractSerializer.Instance.Parse(dto));
			Log(JsonConvert.ExportToString(dto));
			Log(TypeSerializer.SerializeToString(dto));
			Log(TextSerializer.SerializeToString(dto));

			RunMultipleTimes(() => DataContractSerializer.Instance.Parse(dto), "DataContractSerializer.Instance.Parse(dto)");
			RunMultipleTimes(() => JsonDataContractSerializer.Instance.Parse(dto), "JsonDataContractSerializer.Instance.Parse(dto)");
			RunMultipleTimes(() => JsonConvert.ExportToString(dto), "JsonConvert.ExportToString(dto)");
			RunMultipleTimes(() => TypeSerializer.SerializeToString(dto), "TypeSerializer.SerializeToString(dto)");
			RunMultipleTimes(() => TextSerializer.SerializeToString(dto), "TextSerializer.SerializeToString(dto)");
		}

		[Test]
		public void Serialize_ArrayDtoWithOrders()
		{
			var dto = DtoFactory.ArrayDtoWithOrders;

			Log(DataContractSerializer.Instance.Parse(dto));
			Log(JsonDataContractSerializer.Instance.Parse(dto));
			Log(JsonConvert.ExportToString(dto));
			Log(TypeSerializer.SerializeToString(dto));
			Log(TextSerializer.SerializeToString(dto));

			RunMultipleTimes(() => DataContractSerializer.Instance.Parse(dto), "DataContractSerializer.Instance.Parse(dto)");
			RunMultipleTimes(() => JsonDataContractSerializer.Instance.Parse(dto), "JsonDataContractSerializer.Instance.Parse(dto)");
			RunMultipleTimes(() => JsonConvert.ExportToString(dto), "JsonConvert.ExportToString(dto)");
			RunMultipleTimes(() => TypeSerializer.SerializeToString(dto), "TypeSerializer.SerializeToString(dto)");
			RunMultipleTimes(() => TextSerializer.SerializeToString(dto), "TextSerializer.SerializeToString(dto)");
		}

		[Test]
		public void Serialize_CustomerOrderArrayDto()
		{
			var dto = DtoFactory.CustomerOrderArrayDto;

			Log(DataContractSerializer.Instance.Parse(dto));
			Log(JsonDataContractSerializer.Instance.Parse(dto));
			Log(JsonConvert.ExportToString(dto));
			Log(TypeSerializer.SerializeToString(dto));
			Log(TextSerializer.SerializeToString(dto));

			RunMultipleTimes(() => DataContractSerializer.Instance.Parse(dto), "DataContractSerializer.Instance.Parse(dto)");
			RunMultipleTimes(() => JsonDataContractSerializer.Instance.Parse(dto), "JsonDataContractSerializer.Instance.Parse(dto)");
			RunMultipleTimes(() => JsonConvert.ExportToString(dto), "JsonConvert.ExportToString(dto)");
			RunMultipleTimes(() => TypeSerializer.SerializeToString(dto), "TypeSerializer.SerializeToString(dto)");
			RunMultipleTimes(() => TextSerializer.SerializeToString(dto), "TextSerializer.SerializeToString(dto)");
		}

		[Test]
		public void Serialize_CustomerOrderListDto()
		{
			var dto = DtoFactory.CustomerOrderListDto;

			Log(DataContractSerializer.Instance.Parse(dto));
			Log(JsonDataContractSerializer.Instance.Parse(dto));
			Log(JsonConvert.ExportToString(dto));
			Log(TypeSerializer.SerializeToString(dto));
			Log(TextSerializer.SerializeToString(dto));

			RunMultipleTimes(() => DataContractSerializer.Instance.Parse(dto), "DataContractSerializer.Instance.Parse(dto)");
			RunMultipleTimes(() => JsonDataContractSerializer.Instance.Parse(dto), "JsonDataContractSerializer.Instance.Parse(dto)");
			RunMultipleTimes(() => JsonConvert.ExportToString(dto), "JsonConvert.ExportToString(dto)");
			RunMultipleTimes(() => TypeSerializer.SerializeToString(dto), "TypeSerializer.SerializeToString(dto)");
			RunMultipleTimes(() => TextSerializer.SerializeToString(dto), "TextSerializer.SerializeToString(dto)");
		}

	}
}