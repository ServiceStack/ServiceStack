using System;
using System.Collections.Generic;
using System.Linq;
using Northwind.Common.DataModel;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;

namespace ServiceStack.Text.Tests.JsvTests
{
    [TestFixture]
    public class TypeSerializerToStringListTests
    {
       [Test]
        public void Can_serialize_values_with_whitespace()
        {
            var list = new List<string> { " A " };
            var jsv = TypeSerializer.SerializeToString(list);
            var deserializedList = TypeSerializer.DeserializeFromString<List<string>>(jsv);
            Assert.That(list.First(), Is.EqualTo(deserializedList.First()));
        }
    }

}