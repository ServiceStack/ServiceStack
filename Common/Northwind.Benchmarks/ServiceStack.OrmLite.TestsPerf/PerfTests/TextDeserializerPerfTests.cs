using System;
using System.Collections.Generic;
using System.IO;
using Jayrock.Json.Conversion;
using Northwind.Common.ComplexModel;
using Northwind.Common.ServiceModel;
using Northwind.Perf;
using NUnit.Framework;
using Platform.Text;
using ServiceStack.Common.Text;
using ServiceStack.Common.Utils;
using ServiceStack.ServiceModel.Serialization;

namespace ServiceStack.OrmLite.TestsPerf.PerfTests
{
	//[Ignore("Slow performance tests")]
	[TestFixture]
	public class TextDeserializerPerfTests
		: PerfTestBase
	{
		public TextDeserializerPerfTests()
		{
			this.MultipleIterations = new List<int> { 1000, 10000 };			
		}

		public void WriteLog()
		{
			var fileName = string.Format("~/App_Data/PerfTests/TextDeserializer.{0:yyyy-MM-dd}.log", DateTime.Now).MapAbsolutePath();
			using (var writer = new StreamWriter(fileName, true))
			{
				writer.Write(SbLog);
			}
		}

		protected void RunMultipleTimes(ScenarioBase scenarioBase)
		{
			RunMultipleTimes(scenarioBase.Run, scenarioBase.GetType().Name);
		}

		[Test]
		public void Deserialize_Customer()
		{
			var dto = DtoFactory.CustomerDto;

			var dtoXml = DataContractSerializer.Instance.Parse(dto);
			var dtoJson = JsonDataContractSerializer.Instance.Parse(dto);
			var dtoJayrock = JsonConvert.ExportToString(dto);
			var dtoString = StringSerializer.SerializeToString(dto);
			var dtoPlatformText = TextSerializer.SerializeToString(dto);

			RunMultipleTimes(() => DataContractDeserializer.Instance.Parse<CustomerDto>(dtoXml), "DataContractDeserializer.Instance.Parse<CustomerDto>(dtoXml)");
			RunMultipleTimes(() => JsonDataContractDeserializer.Instance.Parse<CustomerDto>(dtoJson), "JsonDataContractDeserializer.Instance.Parse<CustomerDto>(dtoJson)");
			RunMultipleTimes(() => JsonConvert.Import(typeof(CustomerDto), dtoJayrock), "JsonConvert.Import(typeof(CustomerDto), dtoJayrock)");
			RunMultipleTimes(() => StringSerializer.DeserializeFromString<CustomerDto>(dtoString), "StringSerializer.DeserializeFromString<CustomerDto>(dtoString)");
			RunMultipleTimes(() => TextSerializer.DeserializeFromString<CustomerDto>(dtoPlatformText), "TextSerializer.DeserializeFromString<CustomerDto>(dtoPlatformText)");
		}

		[Test]
		public void Deserialize_Order()
		{
			var dto = DtoFactory.OrderDto;

			var dtoXml = DataContractSerializer.Instance.Parse(dto);
			var dtoJson = JsonDataContractSerializer.Instance.Parse(dto);
			var dtoString = StringSerializer.SerializeToString(dto);
			var dtoPlatformText = TextSerializer.SerializeToString(dto);

			RunMultipleTimes(() => DataContractDeserializer.Instance.Parse<OrderDto>(dtoXml), "DataContractDeserializer.Instance.Parse<OrderDto>(dtoXml)");
			RunMultipleTimes(() => JsonDataContractDeserializer.Instance.Parse<OrderDto>(dtoJson), "JsonDataContractDeserializer.Instance.Parse<OrderDto>(dtoJson)");
			RunMultipleTimes(() => StringSerializer.DeserializeFromString<OrderDto>(dtoString), "StringSerializer.DeserializeFromString<OrderDto>(dtoString)");
			RunMultipleTimes(() => TextSerializer.DeserializeFromString<OrderDto>(dtoPlatformText), "TextSerializer.DeserializeFromString<OrderDto>(dtoPlatformText)");
		}

		[Test]
		public void Deserialize_Supplier()
		{
			var dto = DtoFactory.SupplierDto;

			var dtoXml = DataContractSerializer.Instance.Parse(dto);
			var dtoJson = JsonDataContractSerializer.Instance.Parse(dto);
			var dtoString = StringSerializer.SerializeToString(dto);
			var dtoPlatformText = TextSerializer.SerializeToString(dto);

			RunMultipleTimes(() => DataContractDeserializer.Instance.Parse<SupplierDto>(dtoXml), "DataContractDeserializer.Instance.Parse<SupplierDto>(dtoXml)");
			RunMultipleTimes(() => JsonDataContractDeserializer.Instance.Parse<SupplierDto>(dtoJson), "JsonDataContractDeserializer.Instance.Parse<SupplierDto>(dtoJson)");
			RunMultipleTimes(() => StringSerializer.DeserializeFromString<SupplierDto>(dtoString), "StringSerializer.DeserializeFromString<SupplierDto>(dtoString)");
			RunMultipleTimes(() => TextSerializer.DeserializeFromString<SupplierDto>(dtoPlatformText), "TextSerializer.DeserializeFromString<SupplierDto>(dtoPlatformText)");
		}

		[Test]
		public void Deserialize_MultiDtoWithOrders()
		{
			var dto = DtoFactory.MultiDtoWithOrders;

			var dtoXml = DataContractSerializer.Instance.Parse(dto);
			var dtoJson = JsonDataContractSerializer.Instance.Parse(dto);
			var dtoString = StringSerializer.SerializeToString(dto);
			var dtoPlatformText = TextSerializer.SerializeToString(dto);

			RunMultipleTimes(() => DataContractDeserializer.Instance.Parse<MultiDtoWithOrders>(dtoXml), "DataContractDeserializer.Instance.Parse<MultiDtoWithOrders>(dtoXml)");
			RunMultipleTimes(() => JsonDataContractDeserializer.Instance.Parse<MultiDtoWithOrders>(dtoJson), "JsonDataContractDeserializer.Instance.Parse<MultiDtoWithOrders>(dtoJson)");
			RunMultipleTimes(() => StringSerializer.DeserializeFromString<MultiDtoWithOrders>(dtoString), "StringSerializer.DeserializeFromString<MultiDtoWithOrders>(dtoString)");
			RunMultipleTimes(() => TextSerializer.DeserializeFromString<MultiDtoWithOrders>(dtoPlatformText), "TextSerializer.DeserializeFromString<MultiDtoWithOrders>(dtoPlatformText)");
		}

		[Test]
		public void Deserialize_ArrayDtoWithOrders()
		{
			var dto = DtoFactory.ArrayDtoWithOrders;

			var dtoXml = DataContractSerializer.Instance.Parse(dto);
			var dtoJson = JsonDataContractSerializer.Instance.Parse(dto);
			var dtoString = StringSerializer.SerializeToString(dto);
			var dtoPlatformText = TextSerializer.SerializeToString(dto);

			RunMultipleTimes(() => DataContractDeserializer.Instance.Parse<ArrayDtoWithOrders>(dtoXml), "DataContractDeserializer.Instance.Parse<ArrayDtoWithOrders>(dtoXml)");
			RunMultipleTimes(() => JsonDataContractDeserializer.Instance.Parse<ArrayDtoWithOrders>(dtoJson), "JsonDataContractDeserializer.Instance.Parse<ArrayDtoWithOrders>(dtoJson)");
			RunMultipleTimes(() => StringSerializer.DeserializeFromString<ArrayDtoWithOrders>(dtoString), "StringSerializer.DeserializeFromString<ArrayDtoWithOrders>(dtoString)");
			RunMultipleTimes(() => TextSerializer.DeserializeFromString<ArrayDtoWithOrders>(dtoPlatformText), "TextSerializer.DeserializeFromString<ArrayDtoWithOrders>(dtoPlatformText)");
		}

		[Test]
		public void Deserialize_CustomerOrderListDto()
		{
			var dto = DtoFactory.CustomerOrderListDto;

			var dtoXml = DataContractSerializer.Instance.Parse(dto);
			var dtoJson = JsonDataContractSerializer.Instance.Parse(dto);
			var dtoString = StringSerializer.SerializeToString(dto);
			var dtoPlatformText = TextSerializer.SerializeToString(dto);

			RunMultipleTimes(() => DataContractDeserializer.Instance.Parse<CustomerOrderListDto>(dtoXml), "DataContractDeserializer.Instance.Parse<CustomerOrderListDto>(dtoXml)");
			RunMultipleTimes(() => JsonDataContractDeserializer.Instance.Parse<CustomerOrderListDto>(dtoJson), "JsonDataContractDeserializer.Instance.Parse<CustomerOrderListDto>(dtoJson)");
			RunMultipleTimes(() => StringSerializer.DeserializeFromString<CustomerOrderListDto>(dtoString), "StringSerializer.DeserializeFromString<CustomerOrderListDto>(dtoString)");
			RunMultipleTimes(() => TextSerializer.DeserializeFromString<CustomerOrderListDto>(dtoPlatformText), "TextSerializer.DeserializeFromString<CustomerOrderListDto>(dtoPlatformText)");
		}

		[Test]
		public void Deserialize_CustomerOrderArrayDto()
		{
			var dto = DtoFactory.CustomerOrderArrayDto;

			var dtoXml = DataContractSerializer.Instance.Parse(dto);
			var dtoJson = JsonDataContractSerializer.Instance.Parse(dto);
			//var dtoJayrock = JsonConvert.ExportToString(dto);
			var dtoString = StringSerializer.SerializeToString(dto);
			var dtoPlatformText = TextSerializer.SerializeToString(dto);

			RunMultipleTimes(() => DataContractDeserializer.Instance.Parse<CustomerOrderArrayDto>(dtoXml), "DataContractDeserializer.Instance.Parse<CustomerOrderArrayDto>(dtoXml)");
			RunMultipleTimes(() => JsonDataContractDeserializer.Instance.Parse<CustomerOrderArrayDto>(dtoJson), "JsonDataContractDeserializer.Instance.Parse<CustomerOrderArrayDto>(dtoJson)");
			//RunMultipleTimes(() => JsonConvert.Import(typeof(CustomerOrderArrayDto), dtoJayrock), "JsonConvert.Import(typeof(CustomerOrderArrayDto), dtoJayrock)");  #doesn't do nullables
			RunMultipleTimes(() => StringSerializer.DeserializeFromString<CustomerOrderArrayDto>(dtoString), "StringSerializer.DeserializeFromString<CustomerOrderArrayDto>(dtoString)");
			RunMultipleTimes(() => TextSerializer.DeserializeFromString<CustomerOrderArrayDto>(dtoPlatformText), "TextSerializer.DeserializeFromString<CustomerOrderArrayDto>(dtoPlatformText)");
		}
	}
}
