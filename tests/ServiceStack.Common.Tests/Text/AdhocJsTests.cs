using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests.Text
{
	public enum EnumValues
	{
		Enum1,
		Enum2,
		Enum3,
	}

	[TestFixture]
	public class AdhocJsTests
	{
		[Test]
		public void Can_Deserialize()
		{
			var items = TypeSerializer.DeserializeFromString<List<string>>(
				"/CustomPath35/api,/CustomPath40/api,/RootPath35,/RootPath40,:82,:83,:5001/api,:5002/api,:5003,:5004");

			Console.WriteLine(items.Dump());
		}

		[Test]
		public void Can_Serialize_Array_of_enums()
		{
			var enumArr = new[] { EnumValues.Enum1, EnumValues.Enum2, EnumValues.Enum3, };
			var json = JsonSerializer.SerializeToString(enumArr);
			Assert.That(json, Is.EqualTo("[\"Enum1\",\"Enum2\",\"Enum3\"]"));
		}

		[Test]
		public void Can_Serialize_Array_of_chars()
		{
			var enumArr = new[] { 'A', 'B', 'C', };
			var json = JsonSerializer.SerializeToString(enumArr);
			Assert.That(json, Is.EqualTo("[\"A\",\"B\",\"C\"]"));
		}

		[Test]
		public void Can_Serialize_Array_with_nulls()
		{
            using (JsConfig.With(includeNullValues:true))
            {
                var t = new {
                    Name = "MyName",
                    Number = (int?)null,
                    Data = new object[] { 5, null, "text" }
                };

                var json = JsonSerializer.SerializeToString(t);
                Assert.That(json, Is.EqualTo("{\"Name\":\"MyName\",\"Number\":null,\"Data\":[5,null,\"text\"]}"));
            }
		}

		class A
		{
			public string Value { get; set; }
		}

		[Test]
		public void DumpFail()
		{
			var arrayOfA = new[] { new A { Value = "a" }, null, new A { Value = "b" } };
			Console.WriteLine(arrayOfA.Dump());
		}

		[Test]
		public void Deserialize_array_with_null_elements()
		{
			var json = "[{\"Value\": \"a\"},null,{\"Value\": \"b\"}]";
			var o = JsonSerializer.DeserializeFromString<A[]>(json);
			o.PrintDump();
		}
	}
}