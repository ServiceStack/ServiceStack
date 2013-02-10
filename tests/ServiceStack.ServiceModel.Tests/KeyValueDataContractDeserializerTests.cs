using NUnit.Framework;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.ServiceModel.Tests.DataContracts.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using ServiceStack.Text;

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

            [DataMember(Name = "age")]
            public int? Age { get; set; }

            [DataMember(Name = "dob")]
            public DateTime? DOB { get; set; }
        }

        [Test]
        public void KVP_Serializer_does_use_DataMember_Name_attribute()
        {
            var valueMap = new Dictionary<string, string> { { "first_name", "james" }, { "last_name", "bond" } };

            var result = (Customer)KeyValueDataContractDeserializer.Instance.Parse(valueMap, typeof(Customer));

            Assert.That(result.FirstName, Is.EqualTo("james"));
            Assert.That(result.LastName, Is.EqualTo("bond"));
        }

        [Test]
        public void KVP_Serializer_does_not_set_nullable_properties_when_values_are_empty()
        {
            var valueMap = new Dictionary<string, string> { { "first_name", "james" }, { "last_name", "bond" }, { "age", "" }, { "dob", "" } };

            var result = (Customer)KeyValueDataContractDeserializer.Instance.Parse(valueMap, typeof(Customer));

            Assert.That(result.Age, Is.Null);
            Assert.That(result.DOB, Is.Null);
        }

        public class Customer2
        {
            public string FirstName { get; set; }

            public string LastName { get; set; }

            public string FullName
            {
                get { return FirstName + " " + LastName; }
            }
        }

        [Test]
        public void KVP_Serializer_ignores_readonly_properties()
        {
            var valueMap = new Dictionary<string, string> { { "FirstName", "james" }, { "LastName", "bond" }, { "FullName", "james bond" } };
            var result = (Customer2)KeyValueDataContractDeserializer.Instance.Parse(valueMap, typeof(Customer2));
            Assert.That(result.FirstName, Is.EqualTo("james"));
            Assert.That(result.LastName, Is.EqualTo("bond"));
        }

        public class CustomerWithFields
        {
            public readonly string FirstName;

            public readonly string LastName;

            public string FullName
            {
                get { return FirstName + " " + LastName; }
            }

            public CustomerWithFields(string firstName, string lastName)
            {
                FirstName = firstName;
                LastName = lastName;
            }
        }

        [Test]
        public void KVP_Serializer_fills_public_fields()
        {
            using (JsConfig.With(includePublicFields:true))
            {
                var valueMap = new Dictionary<string, string> { { "FirstName", "james" }, { "LastName", "bond" }, { "FullName", "james bond" } };
                var result = (CustomerWithFields)KeyValueDataContractDeserializer.Instance.Parse(valueMap, typeof(CustomerWithFields));
                Assert.That(result.FirstName, Is.EqualTo("james"));
                Assert.That(result.LastName, Is.EqualTo("bond"));
            }
        }
    }
}
