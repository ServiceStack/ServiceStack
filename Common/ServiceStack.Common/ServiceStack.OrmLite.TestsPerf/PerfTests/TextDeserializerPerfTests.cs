using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ServiceStack.Client;
using ServiceStack.Common.Tests.Perf;
using ServiceStack.Common.Text;
using ServiceStack.Common.Utils;
using ServiceStack.OrmLite.Tests.Models;
using ServiceStack.OrmLite.TestsPerf.Model;
using ServiceStack.OrmLite.TestsPerf.Scenarios;

namespace ServiceStack.OrmLite.TestsPerf.PerfTests
{
	[Ignore("Slow performance tests")]
	[TestFixture]
	public class TextDeserializerPerfTests
		: PerfTestBase
	{
		public TextDeserializerPerfTests()
		{
			this.MultipleIterations = new List<int> { 1000, 10000, 100000 };			
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

			var dtoString = StringConverterUtils.ToString(dto);
			var dtoXml = DataContractSerializer.Instance.Parse(dto);
			var dtoJson = JsonDataContractSerializer.Instance.Parse(dto);

			RunMultipleTimes(() => StringConverterUtils.Parse<CustomerDto>(dtoString), "StringConverterUtils.Parse<CustomerDto>(dtoString)");
			RunMultipleTimes(() => DataContractDeserializer.Instance.Parse<CustomerDto>(dtoXml), "DataContractDeserializer.Instance.Parse<CustomerDto>(dtoXml)");
			RunMultipleTimes(() => JsonDataContractDeserializer.Instance.Parse<CustomerDto>(dtoJson), "JsonDataContractDeserializer.Instance.Parse<CustomerDto>(dtoJson)");
		}

		[Test]
		public void Deserialize_Order()
		{
			var dto = DtoFactory.OrderDto;

			var dtoString = StringConverterUtils.ToString(dto);
			var dtoXml = DataContractSerializer.Instance.Parse(dto);
			var dtoJson = JsonDataContractSerializer.Instance.Parse(dto);

			RunMultipleTimes(() => StringConverterUtils.Parse<OrderDto>(dtoString), "StringConverterUtils.Parse<OrderDto>(dtoString)");
			RunMultipleTimes(() => DataContractDeserializer.Instance.Parse<OrderDto>(dtoXml), "DataContractDeserializer.Instance.Parse<OrderDto>(dtoXml)");
			RunMultipleTimes(() => JsonDataContractDeserializer.Instance.Parse<OrderDto>(dtoJson), "JsonDataContractDeserializer.Instance.Parse<OrderDto>(dtoJson)");
		}

		[Test]
		public void Deserialize_Supplier()
		{
			var dto = DtoFactory.SupplierDto;

			var dtoString = StringConverterUtils.ToString(dto);
			var dtoXml = DataContractSerializer.Instance.Parse(dto);
			var dtoJson = JsonDataContractSerializer.Instance.Parse(dto);

			RunMultipleTimes(() => StringConverterUtils.Parse<SupplierDto>(dtoString), "StringConverterUtils.Parse<SupplierDto>(dtoString)");
			RunMultipleTimes(() => DataContractDeserializer.Instance.Parse<SupplierDto>(dtoXml), "DataContractDeserializer.Instance.Parse<SupplierDto>(dtoXml)");
			RunMultipleTimes(() => JsonDataContractDeserializer.Instance.Parse<SupplierDto>(dtoJson), "JsonDataContractDeserializer.Instance.Parse<SupplierDto>(dtoJson)");
		}

		[Test]
		public void Deserialize_MultiDtoWithOrders()
		{
			var dto = DtoFactory.MultiDtoWithOrders;

			var dtoString = StringConverterUtils.ToString(dto);
			var dtoXml = DataContractSerializer.Instance.Parse(dto);
			var dtoJson = JsonDataContractSerializer.Instance.Parse(dto);

			RunMultipleTimes(() => StringConverterUtils.Parse<MultiDtoWithOrders>(dtoString), "StringConverterUtils.Parse<MultiDtoWithOrders>(dtoString)");
			RunMultipleTimes(() => DataContractDeserializer.Instance.Parse<MultiDtoWithOrders>(dtoXml), "DataContractDeserializer.Instance.Parse<MultiDtoWithOrders>(dtoXml)");
			RunMultipleTimes(() => JsonDataContractDeserializer.Instance.Parse<MultiDtoWithOrders>(dtoJson), "JsonDataContractDeserializer.Instance.Parse<MultiDtoWithOrders>(dtoJson)");
		}

		[Test]
		public void Deserialize_ArrayDtoWithOrders()
		{
			var dto = DtoFactory.ArrayDtoWithOrders;

			var dtoString = StringConverterUtils.ToString(dto);
			var dtoXml = DataContractSerializer.Instance.Parse(dto);
			var dtoJson = JsonDataContractSerializer.Instance.Parse(dto);

			RunMultipleTimes(() => StringConverterUtils.Parse<ArrayDtoWithOrders>(dtoString), "StringConverterUtils.Parse<ArrayDtoWithOrders>(dtoString)");
			RunMultipleTimes(() => DataContractDeserializer.Instance.Parse<ArrayDtoWithOrders>(dtoXml), "DataContractDeserializer.Instance.Parse<ArrayDtoWithOrders>(dtoXml)");
			RunMultipleTimes(() => JsonDataContractDeserializer.Instance.Parse<ArrayDtoWithOrders>(dtoJson), "JsonDataContractDeserializer.Instance.Parse<ArrayDtoWithOrders>(dtoJson)");
		}

		[Test]
		public void Deserialize_CustomerOrderListDto()
		{
			var dto = DtoFactory.CustomerOrderListDto;

			var dtoString = StringConverterUtils.ToString(dto);
			var dtoXml = DataContractSerializer.Instance.Parse(dto);
			var dtoJson = JsonDataContractSerializer.Instance.Parse(dto);

			RunMultipleTimes(() => StringConverterUtils.Parse<CustomerOrderListDto>(dtoString), "StringConverterUtils.Parse<CustomerOrderListDto>(dtoString)");
			RunMultipleTimes(() => DataContractDeserializer.Instance.Parse<CustomerOrderListDto>(dtoXml), "DataContractDeserializer.Instance.Parse<CustomerOrderListDto>(dtoXml)");
			RunMultipleTimes(() => JsonDataContractDeserializer.Instance.Parse<CustomerOrderListDto>(dtoJson), "JsonDataContractDeserializer.Instance.Parse<CustomerOrderListDto>(dtoJson)");
		}

		[Test]
		public void Deserialize_CustomerOrderArrayDto()
		{
			var dto = DtoFactory.CustomerOrderArrayDto;

			var dtoString = StringConverterUtils.ToString(dto);
			var dtoXml = DataContractSerializer.Instance.Parse(dto);
			var dtoJson = JsonDataContractSerializer.Instance.Parse(dto);

			RunMultipleTimes(() => StringConverterUtils.Parse<CustomerOrderArrayDto>(dtoString), "StringConverterUtils.Parse<CustomerOrderArrayDto>(dtoString)");
			RunMultipleTimes(() => DataContractDeserializer.Instance.Parse<CustomerOrderArrayDto>(dtoXml), "DataContractDeserializer.Instance.Parse<CustomerOrderArrayDto>(dtoXml)");
			RunMultipleTimes(() => JsonDataContractDeserializer.Instance.Parse<CustomerOrderArrayDto>(dtoJson), "JsonDataContractDeserializer.Instance.Parse<CustomerOrderArrayDto>(dtoJson)");
		}
	}
}
