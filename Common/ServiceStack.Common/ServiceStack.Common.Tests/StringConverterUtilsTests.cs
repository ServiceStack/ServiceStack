using System;
using System.Linq;
using System.Runtime.Serialization;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Common.Utils;
using ServiceStack.Validation.Validators;
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
			[NotNull]
			public string Member1 { get; set; }

			public string Member2 { get; set; }

			[NotNull]
			public string Member3 { get; set; }

			[RequiredText]
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
			string expectedString = default(TestEnum?).ToString();
			Assert.That(stringValue, Is.EqualTo(expectedString));
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
		public void Can_convert_string_dictionary_as_object()
		{
			var stringDictionary = new Dictionary<string, string> {
				{ "One", "1st" }, { "Two", "2nd" }, { "Three", "3rd" }
			};
			var expectedString = "One:1st,Two:2nd,Three:3rd";
			var stringValue = StringConverterUtils.ToString((object)stringDictionary);
			Assert.That(stringValue, Is.EqualTo(expectedString));
		}
	}
}
