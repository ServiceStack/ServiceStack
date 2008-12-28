using System;
using System.Linq;
using ServiceStack.Common.Services.Tests.Support.DataContracts;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Common.Utils;

namespace ServiceStack.Serialization.Tests
{
	[TestFixture]
	public class StringConverterUtilsTests
	{
		[Test]
		public void Create_super_list_type_of_int_from_string()
		{
			var textValue = "1,2,3";
			var convertedValue = textValue.Split(',').ToList().ConvertAll(x => Convert.ToInt32(x));
			var result = StringConverterUtils.Parse<ArrayOfIntId>(textValue);
			Assert.That(result, Is.EquivalentTo(convertedValue));
		}

		[Test]
		public void Create_guid_from_string()
		{
			var textValue = "40DFA5A2-8054-4b3e-B7F5-06E61FF387EF";
			var convertedValue = new Guid(textValue);
			var result = StringConverterUtils.Parse<Guid>(textValue);
			Assert.That(result, Is.EqualTo(convertedValue));
		}

		[Test]
		public void Create_int_from_string()
		{
			var textValue = "99";
			var convertedValue = int.Parse(textValue);
			var result = StringConverterUtils.Parse<int>(textValue);
			Assert.That(result, Is.EqualTo(convertedValue));
		}

		[Test]
		public void Create_bool_from_string()
		{
			var textValue = "True";
			var convertedValue = bool.Parse(textValue);
			var result = StringConverterUtils.Parse<bool>(textValue);
			Assert.That(result, Is.EqualTo(convertedValue));
		}

	}
}