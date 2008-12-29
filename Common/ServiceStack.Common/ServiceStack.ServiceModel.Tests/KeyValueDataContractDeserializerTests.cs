using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.ServiceModel.Tests.DataContracts;

namespace ServiceStack.ServiceModel.Tests
{
	[TestFixture]
	public class KeyValueDataContractDeserializerTests
	{
		[Test]
		public void create_dto_request_from_ids()
		{
			var dtoType = typeof(GetCustomers);
			var textValue = "1,2,3";
			var convertedValue = textValue.Split(',').ToList().ConvertAll(x => Convert.ToInt32(x));
			var valueMap = new Dictionary<string, string> { {"Ids", textValue} };
			var result = (GetCustomers)KeyValueDataContractDeserializer.Instance.Parse(valueMap, dtoType);
			Assert.That(result.CustomerIds, Is.EquivalentTo(convertedValue));
		}
	}
}