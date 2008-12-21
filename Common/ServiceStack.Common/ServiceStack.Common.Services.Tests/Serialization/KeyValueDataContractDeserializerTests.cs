using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Common.Services.Serialization;
using ServiceStack.Common.Services.Tests.Support.DataContracts;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace ServiceStack.Common.Services.Tests.Serialization
{
    [TestFixture]
    public class KeyValueDataContractDeserializerTests
    {
        [Test]
        public void create_dto_request_from_ids()
        {
            var dtoType = typeof(GetUsers);
            var textValue = "1,2,3";
            var convertedValue = textValue.Split(',').ToList().ConvertAll(x => Convert.ToInt32(x));
            var valueMap = new Dictionary<string, string> { {"Ids", textValue} };
            var result = (GetUsers)KeyValueDataContractDeserializer.Instance.Parse(valueMap, dtoType);
            Assert.That(result.Ids, Is.EquivalentTo(convertedValue));
        }
    }
}