using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Northwind.Common.ComplexModel;
using Northwind.Common.DataModel;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Common.Text;
using ServiceStack.Common.Utils;
using System.Collections.Generic;

namespace ServiceStack.Common.Tests.Text
{
	[TestFixture]
	public class StringConverterUtilsTests
	{
		readonly string[] stringValues = new[] { "One", "Two", "Three", "Four", "Five" };
		readonly string[] stringValuesWithIllegalChar = new[] { "One", ",", "Three", "Four", "Five" };

		public enum TestEnum
		{
			EnumValue1,
			EnumValue2,
		}

		public class TestClass
		{
			[Required]
			public string Member1 { get; set; }

			public string Member2 { get; set; }

			[Required]
			public string Member3 { get; set; }

			[StringLength(1)]
			public string Member4 { get; set; }
		}

		[Test]
		public void Can_convert_comma_delimited_string_to_List_String()
		{
			Assert.That(StringSerializer.CanCreateFromString(typeof(List<string>)), Is.True);

			var stringValueList = "[" + string.Join(",", stringValues) + "]";

			var convertedStringValues = StringSerializer.DeserializeFromString<List<string>>(stringValueList);
			Assert.That(convertedStringValues, Is.EquivalentTo(stringValues));
		}

		[Test]
		public void Empty_string_returns_empty_List_String()
		{
			var convertedStringValues = StringSerializer.DeserializeFromString<List<string>>(string.Empty);
			Assert.That(convertedStringValues, Is.EqualTo(new List<string>()));
		}

		[Test]
		public void Null_string_returns_empty_List_String()
		{
			var convertedStringValues = StringSerializer.DeserializeFromString<List<string>>(null);
			Assert.That(convertedStringValues, Is.EqualTo(new List<string>()));
		}

		[Test]
		public void Empty_string_returns_empty_Map_String()
		{
			var convertedStringValues = StringSerializer.DeserializeFromString<Dictionary<string, string>>(string.Empty);
			Assert.That(convertedStringValues, Is.EqualTo(new Dictionary<string, string>()));
		}

		[Test]
		public void Null_string_returns_empty_Map_String()
		{
			var convertedStringValues = StringSerializer.DeserializeFromString<Dictionary<string, string>>(null);
			Assert.That(convertedStringValues, Is.EqualTo(new Dictionary<string, string>()));
		}

		[Test]
		public void Can_convert_string_collection()
		{
			Assert.That(StringSerializer.CanCreateFromString(typeof(string[])), Is.True);

			var stringValue = StringSerializer.SerializeToString(stringValues);
			var expectedString = "[" + string.Join(",", stringValues.ToArray()) + "]";
			Assert.That(stringValue, Is.EqualTo(expectedString));
		}

		[Test]
		public void Can_convert_enum()
		{
			var enumValue = TestEnum.EnumValue1;
			var stringValue = StringSerializer.SerializeToString(enumValue);
			var expectedString = TestEnum.EnumValue1.ToString();
			Assert.That(stringValue, Is.EqualTo(expectedString));
		}

		[Test]
		public void Can_convert_nullable_enum()
		{
			TestEnum? enumValue = TestEnum.EnumValue1;
			var stringValue = StringSerializer.SerializeToString(enumValue);
			var expectedString = TestEnum.EnumValue1.ToString();
			Assert.That(stringValue, Is.EqualTo(expectedString));
		}

		[Test]
		public void Can_convert_to_nullable_enum()
		{
			Assert.That(StringSerializer.CanCreateFromString(typeof(TestEnum?)), Is.True);

			TestEnum? enumValue = TestEnum.EnumValue1;
			var actualValue = StringSerializer.DeserializeFromString<TestEnum?>(enumValue.ToString());
			Assert.That(actualValue, Is.EqualTo(enumValue));
		}

		[Test]
		public void Can_convert_to_nullable_enum_with_null_value()
		{
			var enumValue = StringSerializer.DeserializeFromString<TestEnum?>(null);
			Assert.That(enumValue, Is.Null);
		}

		[Test]
		public void Can_convert_nullable_enum_with_null_value()
		{
			TestEnum? enumValue = null;
			var stringValue = StringSerializer.SerializeToString(enumValue);
			Assert.That(stringValue, Is.Null);
		}

		[Test]
		public void Can_convert_Guid()
		{
			Assert.That(StringSerializer.CanCreateFromString(typeof(Guid)), Is.True);

			var guidValue = Guid.NewGuid();
			var stringValue = StringSerializer.SerializeToString(guidValue);
			var expectedString = guidValue.ToString();
			Assert.That(stringValue, Is.EqualTo(expectedString));
		}

		[Test]
		public void Can_convert_datetime()
		{
			var dateValue = new DateTime(1979, 5, 9);
			var stringValue = StringSerializer.SerializeToString(dateValue);
			var expectedString = dateValue.ToString();
			Assert.That(stringValue, Is.EqualTo(expectedString));
		}

		[Test]
		public void Can_convert_to_datetime()
		{
			Assert.That(StringSerializer.CanCreateFromString(typeof(DateTime)), Is.True);

			var dateValue = new DateTime(1979, 5, 9);
			var actualValue = StringSerializer.DeserializeFromString<DateTime>(dateValue.ToString());
			Assert.That(actualValue, Is.EqualTo(dateValue));
		}

		[Test]
		public void Can_convert_nullable_datetime()
		{
			DateTime? dateValue = new DateTime(1979, 5, 9);
			var stringValue = StringSerializer.SerializeToString(dateValue);
			var expectedString = dateValue.ToString();
			Assert.That(stringValue, Is.EqualTo(expectedString));
		}

		[Test]
		public void Can_convert_to_nullable_datetime()
		{
			Assert.That(StringSerializer.CanCreateFromString(typeof(DateTime?)), Is.True);

			DateTime? dateValue = new DateTime(1979, 5, 9);
			var actualValue = StringSerializer.DeserializeFromString<DateTime?>(dateValue.ToString());
			Assert.That(actualValue, Is.EqualTo(dateValue));
		}

		[Test]
		public void Can_convert_string_List()
		{
			var stringValue = StringSerializer.SerializeToString(stringValues.ToList());
			var expectedString = "[" + string.Join(",", stringValues.ToArray()) + "]";
			Assert.That(stringValue, Is.EqualTo(expectedString));
		}

		[Test]
		public void Can_convert_string_array()
		{
			var stringValue = StringSerializer.SerializeToString(stringValues.ToArray());
			var expectedString = "[" + string.Join(",", stringValues.ToArray()) + "]";
			Assert.That(stringValue, Is.EqualTo(expectedString));
		}

		[Test]
		public void Can_convert_string_List_as_object()
		{
			var stringValue = StringSerializer.SerializeToString((object)stringValues.ToList());
			var expectedString = "[" + string.Join(",", stringValues.ToArray()) + "]";
			Assert.That(stringValue, Is.EqualTo(expectedString));
		}

		[Test]
		public void Can_convert_empty_List()
		{
			var stringValue = StringSerializer.SerializeToString(new List<string>());
			var expectedString = "[]";
			Assert.That(stringValue, Is.EqualTo(expectedString));
		}

		[Test]
		public void Can_convert_empty_List_as_object()
		{
			var stringValue = StringSerializer.SerializeToString((object)new List<string>());
			var expectedString = "[]";
			Assert.That(stringValue, Is.EqualTo(expectedString));
		}

		[Test]
		public void Can_convert_string_dictionary()
		{
			var stringDictionary = new Dictionary<string, string> {
			                                                      	{ "One", "1st" }, { "Two", "2nd" }, { "Three", "3rd" }
			                                                      };
			var expectedString = "{One:1st,Two:2nd,Three:3rd}";
			var stringValue = StringSerializer.SerializeToString(stringDictionary);
			Assert.That(stringValue, Is.EqualTo(expectedString));
		}

		[Test]
		public void Can_parse_string_dictionary()
		{
			var stringDictionary = new Dictionary<string, string> {
				{ "One", "1st" }, { "Two", "2nd" }, { "Three", "3rd" }
			};
			const string mapValues = "{One:1st,Two:2nd,Three:3rd}";
			var parsedDictionary = StringSerializer.DeserializeFromString(mapValues, stringDictionary.GetType());
			Assert.That(parsedDictionary, Is.EquivalentTo(stringDictionary));
		}

		[Test]
		public void Can_convert_string_dictionary_as_object()
		{
			var stringDictionary = new Dictionary<string, string> {
				{ "One", "1st" }, { "Two", "2nd" }, { "Three", "3rd" }
			};
			var expectedString = "{One:1st,Two:2nd,Three:3rd}";
			var stringValue = StringSerializer.SerializeToString((object)stringDictionary);
			Assert.That(stringValue, Is.EqualTo(expectedString));
		}

		[Test]
		public void Can_convert_string_dictionary_with_special_chars_as_object()
		{
			var stringDictionary = new Dictionary<string, string> {
				{ "One", "\"1st" }, { "Two", "2:nd" }, { "Three", "3r,d" }, { "Four", "four%" }
			};
			var expectedString = "{One:\"\"\"1st\",Two:2:nd,Three:\"3r,d\",Four:four%}";
			var stringValue = StringSerializer.SerializeToString(stringDictionary);
			Assert.That(stringValue, Is.EqualTo(expectedString));
		}

		[Test]
		public void Can_parse_string_dictionary_with_special_chars_as_object()
		{
			var stringDictionary = new Dictionary<string, string> {
				{ "One", "\"1st" }, { "Two", "2:nd" }, { "Three", "3r,d" }
			};
			const string mapValues = "{One:\"\"\"1st\",Two:2:nd,Three:\"3r,d\"}";
			var parsedDictionary = StringSerializer.DeserializeFromString(mapValues, stringDictionary.GetType());
			Assert.That(parsedDictionary, Is.EquivalentTo(stringDictionary));
		}

		[Test]
		public void Can_convert_string_list_with_special_chars_as_object()
		{
			var stringList = new List<string> {
				"\"1st", "2:nd", "3r,d", "four%"
			};
			var expectedString = "[\"\"\"1st\",2:nd,\"3r,d\",four%]";
			var stringValue = StringSerializer.SerializeToString(stringList);
			Assert.That(stringValue, Is.EqualTo(expectedString));
		}

		[Test]
		public void Can_parse_string_list_with_special_chars_as_object()
		{
			var stringList = new List<string> {
          		"\"1st", "2:nd", "3r,d", "four%"
			  };
			const string listValues = "[\"\"\"1st\",2:nd,\"3r,d\",four%]";
			var parsedList = StringSerializer.DeserializeFromString(listValues, stringList.GetType());
			Assert.That(parsedList, Is.EquivalentTo(stringList));
		}


		[Test]
		public void Can_convert_Byte_array()
		{
			var byteArrayValue = new byte[] { 0, 65, 97, 255, };
			var stringValue = StringSerializer.SerializeToString(byteArrayValue);
			var expectedString = System.Text.Encoding.Default.GetString(byteArrayValue);
			Assert.That(stringValue, Is.EqualTo(expectedString));
		}

		[Test]
		public void Can_convert_to_Byte_array()
		{
			Assert.That(StringSerializer.CanCreateFromString(typeof(byte[])), Is.True);

			var byteArrayValue = new byte[] { 0, 65, 97, 255, };
			var byteArrayString = StringSerializer.SerializeToString(byteArrayValue);
			var actualValue = StringSerializer.DeserializeFromString<byte[]>(byteArrayString);
			Assert.That(actualValue, Is.EqualTo(byteArrayValue));
		}

		public T ConvertModel<T>(T model)
		{
			var strModel = StringSerializer.SerializeToString(model);
			Console.WriteLine(strModel);
			var toModel = StringSerializer.DeserializeFromString<T>(strModel);
			return toModel;
		}

		[Test]
		public void Can_convert_ModelWithFieldsOfDifferentTypes()
		{
			var model = ModelWithFieldsOfDifferentTypes.Create(1);
			var toModel = ConvertModel(model);

			ModelWithFieldsOfDifferentTypes.AssertIsEqual(toModel, model);
		}

		[Test]
		public void Can_convert_ModelWithFieldsOfNullableTypes()
		{
			var model = ModelWithFieldsOfNullableTypes.Create(1);
			var toModel = ConvertModel(model);

			ModelWithFieldsOfNullableTypes.AssertIsEqual(toModel, model);
		}

		[Test]
		public void Can_convert_ModelWithComplexTypes()
		{
			var model = ModelWithComplexTypes.Create(1);
			var toModel = ConvertModel(model);

			ModelWithComplexTypes.AssertIsEqual(toModel, model);
		}

		[Test]
		public void Can_convert_model_with_TypeChar()
		{
			var model = new ModelWithIdAndName { Id = 1, Name = "in } valid" };
			var toModel = ConvertModel(model);

			ModelWithIdAndName.AssertIsEqual(toModel, model);
		}

		[Test]
		public void Can_convert_model_with_ListChar()
		{
			var model = new ModelWithIdAndName { Id = 1, Name = "in [ valid" };
			var toModel = ConvertModel(model);

			ModelWithIdAndName.AssertIsEqual(toModel, model);
		}

		[Test]
		public void Can_convert_ArrayDtoWithOrders()
		{
			var model = DtoFactory.ArrayDtoWithOrders;
			var toModel = ConvertModel(model);

			Assert.That(model.Equals(toModel), Is.True);
		}
	}
}