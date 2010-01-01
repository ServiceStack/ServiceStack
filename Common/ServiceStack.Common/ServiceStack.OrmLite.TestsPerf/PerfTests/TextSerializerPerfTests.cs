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
	public class TextSerializerPerfTests
		: PerfTestBase
	{
		public TextSerializerPerfTests()
		{
			this.MultipleIterations = new List<int> { 1000, 10000, 100000 };			
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

		[Test]
		public void Run_NorthwindCustomerToStringScenario()
		{
			var customer = NorthwindDtoFactory.Customer(
			1.ToString("x"), "Alfreds Futterkiste", "Maria Anders", "Sales Representative", "Obere Str. 57",
			"Berlin", null, "12209", "Germany", "030-0074321", "030-0076545", null);

			Log(ToStringMethods.ToString(customer));

			RunMultipleTimes(() => StringConverterUtils.ToString(customer), "ToStringMethods.ToString(customer)");
			RunMultipleTimes(() => DataContractSerializer.Instance.Parse(customer), "DataContractSerializer.Instance.Parse(customer)");
			RunMultipleTimes(() => JsonDataContractSerializer.Instance.Parse(customer), "JsonDataContractSerializer.Instance.Parse(customer)");
		}

		[Test]
		public void Run_NorthwindOrderToStringScenario()
		{
			var order = NorthwindDtoFactory.Order(
				1, "VINET", 5, new DateTime(1996, 7, 4), new DateTime(1996, 1, 8), new DateTime(1996, 7, 16), 
				3, 32.38m, "Vins et alcools Chevalier", "59 rue de l'Abbaye", "Reims", null, "51100", "France");

			Log(ToStringMethods.ToString(order));

			RunMultipleTimes(() => StringConverterUtils.ToString(order), "ToStringMethods.ToString(order)");
			RunMultipleTimes(() => DataContractSerializer.Instance.Parse(order), "DataContractSerializer.Instance.Parse(order)");
			RunMultipleTimes(() => JsonDataContractSerializer.Instance.Parse(order), "JsonDataContractSerializer.Instance.Parse(order)");
		}

		[Test]
		public void Run_NorthwindSupplierToStringScenario()
		{
			var supplier = NorthwindDtoFactory.Supplier(
			1, "Exotic Liquids", "Charlotte Cooper", "Purchasing Manager", "49 Gilbert St.", "London", null,
			"EC1 4SD", "UK", "(171) 555-2222", null, null);

			Log(ToStringMethods.ToString(supplier));

			RunMultipleTimes(() => StringConverterUtils.ToString(supplier), "ToStringMethods.ToString(supplier)");
			RunMultipleTimes(() => DataContractSerializer.Instance.Parse(supplier), "DataContractSerializer.Instance.Parse(order)");
			RunMultipleTimes(() => JsonDataContractSerializer.Instance.Parse(supplier), "JsonDataContractSerializer.Instance.Parse(order)");
		}
	}
}
