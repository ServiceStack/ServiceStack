using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;

namespace ServiceStack.Text.Tests.JsvTests;

[TestFixture]
public class TypeSerializerToStringDictionaryTests
{
    [Test]
    public void Can_serialize_ModelWithFieldsOfDifferentTypes_to_StringDictionary()
    {
        var model = new ModelWithFieldsOfDifferentTypes
        {
            Id = 1,
            Name = "Name1",
            LongId = 1000,
            Guid = new Guid("{7da74353-a40c-468e-93aa-7ff51f4f0e84}"),
            Bool = false,
            DateTime = new DateTime(2010, 12, 20),
            Double = 2.11d,
        };

        Console.WriteLine(model.Dump());
        /* Prints out:
        {
            Id: 1,
            Name: Name1,
            LongId: 1000,
            Guid: 7da74353a40c468e93aa7ff51f4f0e84,
            Bool: False,
            DateTime: 2010-12-20,
            Double: 2.11
        }
        */

        Dictionary<string, string> map = model.ToStringDictionary();
        Assert.That(map.EquivalentTo(
            new Dictionary<string, string>
            {
                {"Id","1"},
                {"Name","Name1"},
                {"LongId","1000"},
                {"Guid","7da74353a40c468e93aa7ff51f4f0e84"},
                {"Bool","False"},
                {"DateTime","2010-12-20"},
                {"Double","2.11"},
            }));
    }
}
