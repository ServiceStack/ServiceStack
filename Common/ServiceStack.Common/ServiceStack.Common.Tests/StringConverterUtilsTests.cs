using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Northwind.Common.ComplexModel;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Common.Utils;
using System.Collections.Generic;

namespace ServiceStack.Common.Tests
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
			Assert.That(StringConverterUtils.CanCreateFromString(typeof(List<string>)), Is.True);

			var stringValueList = string.Join(",", stringValues);

			var convertedStringValues = StringConverterUtils.Parse<List<string>>(stringValueList);
			Assert.That(convertedStringValues, Is.EquivalentTo(stringValues));
		}

		[Test]
		public void Empty_string_returns_empty_List_String()
		{
			var convertedStringValues = StringConverterUtils.Parse<List<string>>(string.Empty);
			Assert.That(convertedStringValues, Is.EqualTo(new List<string>()));
		}

		[Test]
		public void Null_string_returns_empty_List_String()
		{
			var convertedStringValues = StringConverterUtils.Parse<List<string>>(null);
			Assert.That(convertedStringValues, Is.EqualTo(new List<string>()));
		}

		[Test]
		public void Can_convert_string_collection()
		{
			Assert.That(StringConverterUtils.CanCreateFromString(typeof(string[])), Is.True);

			var stringValue = StringConverterUtils.ToString(stringValues);
			var expectedString = string.Join(",", stringValues.ToArray());
			Assert.That(stringValue, Is.EqualTo(expectedString));
		}

		[Test]
		public void Can_convert_enum()
		{
			var enumValue = TestEnum.EnumValue1;
			var stringValue = StringConverterUtils.ToString(enumValue);
			var expectedString = TestEnum.EnumValue1.ToString();
			Assert.That(stringValue, Is.EqualTo(expectedString));
		}

		[Test]
		public void Can_convert_nullable_enum()
		{
			TestEnum? enumValue = TestEnum.EnumValue1;
			var stringValue = StringConverterUtils.ToString(enumValue);
			var expectedString = TestEnum.EnumValue1.ToString();
			Assert.That(stringValue, Is.EqualTo(expectedString));
		}

		[Test]
		public void Can_convert_to_nullable_enum()
		{
			Assert.That(StringConverterUtils.CanCreateFromString(typeof(TestEnum?)), Is.True);

			TestEnum? enumValue = TestEnum.EnumValue1;
			var actualValue = StringConverterUtils.Parse<TestEnum?>(enumValue.ToString());
			Assert.That(actualValue, Is.EqualTo(enumValue));
		}

		[Test]
		public void Can_convert_to_nullable_enum_with_null_value()
		{
			var enumValue = StringConverterUtils.Parse<TestEnum?>(null);
			Assert.That(enumValue, Is.Null);
		}

		[Test]
		public void Can_convert_nullable_enum_with_null_value()
		{
			TestEnum? enumValue = null;
			var stringValue = StringConverterUtils.ToString(enumValue);
			Assert.That(stringValue, Is.Null);
		}

		[Test]
		public void Can_convert_Guid()
		{
			Assert.That(StringConverterUtils.CanCreateFromString(typeof(Guid)), Is.True);

			var guidValue = Guid.NewGuid();
			var stringValue = StringConverterUtils.ToString(guidValue);
			var expectedString = guidValue.ToString();
			Assert.That(stringValue, Is.EqualTo(expectedString));
		}

		[Test]
		public void Can_convert_datetime()
		{
			var dateValue = new DateTime(1979, 5, 9);
			var stringValue = StringConverterUtils.ToString(dateValue);
			var expectedString = dateValue.ToString();
			Assert.That(stringValue, Is.EqualTo(expectedString));
		}

		[Test]
		public void Can_convert_to_datetime()
		{
			Assert.That(StringConverterUtils.CanCreateFromString(typeof(DateTime)), Is.True);

			var dateValue = new DateTime(1979, 5, 9);
			var actualValue = StringConverterUtils.Parse<DateTime>(dateValue.ToString());
			Assert.That(actualValue, Is.EqualTo(dateValue));
		}

		[Test]
		public void Can_convert_nullable_datetime()
		{
			DateTime? dateValue = new DateTime(1979, 5, 9);
			var stringValue = StringConverterUtils.ToString(dateValue);
			var expectedString = dateValue.ToString();
			Assert.That(stringValue, Is.EqualTo(expectedString));
		}

		[Test]
		public void Can_convert_to_nullable_datetime()
		{
			Assert.That(StringConverterUtils.CanCreateFromString(typeof(DateTime?)), Is.True);

			DateTime? dateValue = new DateTime(1979, 5, 9);
			var actualValue = StringConverterUtils.Parse<DateTime?>(dateValue.ToString());
			Assert.That(actualValue, Is.EqualTo(dateValue));
		}

		[Test]
		public void Can_convert_string_List()
		{
			var stringValue = StringConverterUtils.ToString(stringValues.ToList());
			var expectedString = string.Join(",", stringValues.ToArray());
			Assert.That(stringValue, Is.EqualTo(expectedString));
		}

		[Test]
		public void Can_convert_string_array()
		{
			var stringValue = StringConverterUtils.ToString(stringValues.ToArray());
			var expectedString = string.Join(",", stringValues.ToArray());
			Assert.That(stringValue, Is.EqualTo(expectedString));
		}

		[Test]
		public void Can_convert_string_List_as_object()
		{
			var stringValue = StringConverterUtils.ToString((object)stringValues.ToList());
			var expectedString = string.Join(",", stringValues.ToArray());
			Assert.That(stringValue, Is.EqualTo(expectedString));
		}

		[Test]
		public void Can_convert_empty_List()
		{
			var stringValue = StringConverterUtils.ToString(new List<string>());
			var expectedString = "";
			Assert.That(stringValue, Is.EqualTo(expectedString));
		}

		[Test]
		public void Can_convert_empty_List_as_object()
		{
			var stringValue = StringConverterUtils.ToString((object)new List<string>());
			var expectedString = "";
			Assert.That(stringValue, Is.EqualTo(expectedString));
		}

		[Test]
		public void Can_convert_string_dictionary()
		{
			var stringDictionary = new Dictionary<string, string> {
				{ "One", "1st" }, { "Two", "2nd" }, { "Three", "3rd" }
			};
			var expectedString = "One:1st,Two:2nd,Three:3rd";
			var stringValue = StringConverterUtils.ToString(stringDictionary);
			Assert.That(stringValue, Is.EqualTo(expectedString));
		}

		[Test]
		public void Can_parse_string_dictionary()
		{
			var stringDictionary = new Dictionary<string, string> {
				{ "One", "1st" }, { "Two", "2nd" }, { "Three", "3rd" }
			};
			const string mapValues = "One:1st,Two:2nd,Three:3rd";
			var parsedDictionary = StringConverterUtils.Parse(mapValues, stringDictionary.GetType());
			Assert.That(parsedDictionary, Is.EquivalentTo(stringDictionary));
		}

		[Test]
		public void Can_convert_string_dictionary_as_object()
		{
			var stringDictionary = new Dictionary<string, string> {
				{ "One", "1st" }, { "Two", "2nd" }, { "Three", "3rd" }
			};
			var expectedString = "One:1st,Two:2nd,Three:3rd";
			var stringValue = StringConverterUtils.ToString((object)stringDictionary);
			Assert.That(stringValue, Is.EqualTo(expectedString));
		}

		[Test]
		public void Can_convert_string_dictionary_with_special_chars_as_object()
		{
			var stringDictionary = new Dictionary<string, string> {
				{ "One", "\"1st" }, { "Two", "2:nd" }, { "Three", "3r,d" }, { "Four", "four%" }
			};
			var expectedString = "One:\"%221st\",Two:\"2%3and\",Three:\"3r%2cd\",Four:four%";
			var stringValue = StringConverterUtils.ToString(stringDictionary);
			Assert.That(stringValue, Is.EqualTo(expectedString));
		}

		[Test]
		public void Can_parse_string_dictionary_with_special_chars_as_object()
		{
			var stringDictionary = new Dictionary<string, string> {
				{ "One", "\"1st" }, { "Two", "2:nd" }, { "Three", "3r,d" }, { "Four", "four%" }
			};
			const string mapValues = "One:\"%221st\",Two:\"2%3and\",Three:\"3r%2cd\",Four:four%";
			var parsedDictionary = StringConverterUtils.Parse(mapValues, stringDictionary.GetType());
			Assert.That(parsedDictionary, Is.EquivalentTo(stringDictionary));
		}

		[Test]
		public void Can_convert_string_list_with_special_chars_as_object()
		{
			var stringList = new List<string> {
				"\"1st", "2:nd", "3r,d", "four%"
			};
			var expectedString = "\"%221st\",\"2%3and\",\"3r%2cd\",four%";
			var stringValue = StringConverterUtils.ToString(stringList);
			Assert.That(stringValue, Is.EqualTo(expectedString));
		}

		[Test]
		public void Can_parse_string_list_with_special_chars_as_object()
		{
			var stringList = new List<string> {
				"\"1st", "2:nd", "3r,d", "four%"
			};
			const string listValues = "\"%221st\",\"2%3and\",\"3r%2cd\",four%";
			var parsedList = StringConverterUtils.Parse(listValues, stringList.GetType());
			Assert.That(parsedList, Is.EquivalentTo(stringList));
		}


		[Test]
		public void Can_convert_Byte_array()
		{
			var byteArrayValue = new byte[]{ 0, 65, 97, 255, };
			var stringValue = StringConverterUtils.ToString(byteArrayValue);
			var expectedString = System.Text.Encoding.Default.GetString(byteArrayValue);
			Assert.That(stringValue, Is.EqualTo(expectedString));
		}

		[Test]
		public void Can_convert_to_Byte_array()
		{
			Assert.That(StringConverterUtils.CanCreateFromString(typeof(byte[])), Is.True);

			var byteArrayValue = new byte[] { 0, 65, 97, 255, };
			var byteArrayString = StringConverterUtils.ToString(byteArrayValue);
			var actualValue = StringConverterUtils.Parse<byte[]>(byteArrayString);
			Assert.That(actualValue, Is.EqualTo(byteArrayValue));
		}

		[Test]
		public void Can_convert_CustomerOrderListDto()
		{
			var dto = DtoFactory.CustomerOrderListDto;

			var dtoString = StringConverterUtils.ToString(dto);
			dtoString = StringConverterUtils.ToString(dto);

			Assert.That(dtoString, Is.Not.Null);
		}

		[Test]
		public void Can_convert_to_CustomerOrderListDto()
		{
			var dto = DtoFactory.CustomerOrderListDto;

			var dtoString = StringConverterUtils.ToString(dto);
			var fromDto = StringConverterUtils.Parse<CustomerOrderListDto>(dtoString);

			Assert.That(fromDto, Is.Not.Null);
		}

	}
}
