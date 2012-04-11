using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using NUnit.Framework;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.ServiceModel.Tests.DataContracts.Operations;

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
			var valueMap = new Dictionary<string, string> { { "CustomerIds", textValue } };
			var result = (GetCustomers)KeyValueDataContractDeserializer.Instance.Parse(valueMap, dtoType);
			Assert.That(result.CustomerIds, Is.EquivalentTo(convertedValue));
		}

        [DataContract]
        public class Customer
        {
            [DataMember(Name = "first_name")]
            public string FirstName { get; set; }

            [DataMember(Name = "last_name")]
            public string LastName { get; set; }
        }

	    [Test]
	    public void KVP_Serializer_does_use_DataMember_Name_attribute()
	    {
            var valueMap = new Dictionary<string, string> { { "first_name", "james" }, { "last_name", "bond" } };

            var result = (Customer)KeyValueDataContractDeserializer.Instance.Parse(valueMap, typeof(Customer));

            Assert.That(result.FirstName, Is.EqualTo("james"));
            Assert.That(result.LastName, Is.EqualTo("bond"));
	    }
	}
}