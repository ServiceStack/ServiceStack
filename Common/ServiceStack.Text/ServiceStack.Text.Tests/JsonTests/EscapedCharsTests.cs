using System;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Tests.Models;

namespace ServiceStack.Text.Tests.JsonTests
{
	[TestFixture]
	public class EscapedCharsTests
		: TestBase
	{
		public class NestedModel
		{
			public string Id { get; set; }

			public ModelWithIdAndName Model { get; set; }
		}


		[Test]
		public void Can_deserialize_text_with_escaped_chars()
		{
			var model = new ModelWithIdAndName
			{
				Id = 1,
				Name = @"1 \ 2 \r 3 \n 4 \b 5 \f 6 """
			};

			SerializeAndCompare(model);
		}

		[Test]
		public void Can_short_circuit_string_with_no_escape_chars()
		{
			var model = new ModelWithIdAndName
			{
				Id = 1,
				Name = @"Simple string"
			};

			SerializeAndCompare(model);
		}

		[Test]
		public void Can_deserialize_json_with_whitespace()
		{
			var model = new ModelWithIdAndName
			{
				Id = 1,
				Name = @"Simple string"
			};

			const string json = "\t { \t \"Id\" \t : 1 , \t \"Name\" \t  : \t \"Simple string\" \t } \t ";

			var fromJson = JsonSerializer.DeserializeFromString<ModelWithIdAndName>(json);

			Assert.That(fromJson, Is.EqualTo(model));
		}

		[Test]
		public void Can_deserialize_nested_json_with_whitespace()
		{
			var model = new NestedModel
          	{
                Id = "Nested with space",
				Model = new ModelWithIdAndName
				{
					Id = 1,
					Name = @"Simple string"
				}
          	};

			const string json = "\t { \"Id\" : \"Nested with space\" \n , \r \t \"Model\" \t : \n { \t \"Id\" \t : 1 , \t \"Name\" \t  : \t \"Simple string\" \t } \t } \n ";

			var fromJson = JsonSerializer.DeserializeFromString<NestedModel>(json);

			Assert.That(fromJson.Id, Is.EqualTo(model.Id));
			Assert.That(fromJson.Model, Is.EqualTo(model.Model));
		}

	}
}