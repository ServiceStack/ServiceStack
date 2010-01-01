using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ServiceStack.Client;
using ServiceStack.Common.Tests.Perf;
using ServiceStack.Common.Text;
using ServiceStack.Common.Utils;
using ServiceStack.OrmLite.Tests.Models;
using ServiceStack.OrmLite.TestsPerf.Scenarios;

namespace ServiceStack.OrmLite.TestsPerf.PerfTests
{
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
		public void Run_NorthwindCustomerToStringScenario()
		{
			var dto = NorthwindDtoFactory.Customer(
			1.ToString("x"), "Alfreds Futterkiste", "Maria Anders", "Sales Representative", "Obere Str. 57",
			"Berlin", null, "12209", "Germany", "030-0074321", "030-0076545", null);

			var dtoString = StringConverterUtils.ToString(dto);
			var dtoXml = DataContractSerializer.Instance.Parse(dto);
			var dtoJson = JsonDataContractSerializer.Instance.Parse(dto);

			RunMultipleTimes(() => StringConverterUtils.Parse<CustomerDto>(dtoString), "StringConverterUtils.Parse<CustomerDto>(dtoString)");
			RunMultipleTimes(() => DataContractDeserializer.Instance.Parse<CustomerDto>(dtoXml), "DataContractDeserializer.Instance.Parse<CustomerDto>(dtoXml)");
			RunMultipleTimes(() => JsonDataContractDeserializer.Instance.Parse<CustomerDto>(dtoJson), "JsonDataContractDeserializer.Instance.Parse<CustomerDto>(dtoJson)");
		}

		[Test]
		public void Run_NorthwindOrderToStringScenario()
		{
			var dto = NorthwindDtoFactory.Order(
				1, "VINET", 5, new DateTime(1996, 7, 4), new DateTime(1996, 1, 8), new DateTime(1996, 7, 16), 
				3, 32.38m, "Vins et alcools Chevalier", "59 rue de l'Abbaye", "Reims", null, "51100", "France");

			var dtoString = StringConverterUtils.ToString(dto);
			var dtoXml = DataContractSerializer.Instance.Parse(dto);
			var dtoJson = JsonDataContractSerializer.Instance.Parse(dto);

			RunMultipleTimes(() => StringConverterUtils.Parse<OrderDto>(dtoString), "StringConverterUtils.Parse<OrderDto>(dtoString)");
			RunMultipleTimes(() => DataContractDeserializer.Instance.Parse<OrderDto>(dtoXml), "DataContractDeserializer.Instance.Parse<OrderDto>(dtoXml)");
			RunMultipleTimes(() => JsonDataContractDeserializer.Instance.Parse<OrderDto>(dtoJson), "JsonDataContractDeserializer.Instance.Parse<OrderDto>(dtoJson)");
		}

		[Test]
		public void Run_NorthwindSupplierToStringScenario()
		{
			var dto = NorthwindDtoFactory.Supplier(
			1, "Exotic Liquids", "Charlotte Cooper", "Purchasing Manager", "49 Gilbert St.", "London", null,
			"EC1 4SD", "UK", "(171) 555-2222", null, null);

			var dtoString = StringConverterUtils.ToString(dto);
			var dtoXml = DataContractSerializer.Instance.Parse(dto);
			var dtoJson = JsonDataContractSerializer.Instance.Parse(dto);

			RunMultipleTimes(() => StringConverterUtils.Parse<SupplierDto>(dtoString), "StringConverterUtils.Parse<SupplierDto>(dtoString)");
			RunMultipleTimes(() => DataContractDeserializer.Instance.Parse<SupplierDto>(dtoXml), "DataContractDeserializer.Instance.Parse<SupplierDto>(dtoXml)");
			RunMultipleTimes(() => JsonDataContractDeserializer.Instance.Parse<SupplierDto>(dtoJson), "JsonDataContractDeserializer.Instance.Parse<SupplierDto>(dtoJson)");
		}
	}
}
