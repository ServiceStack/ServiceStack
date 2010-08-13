using System;
using System.Collections.Generic;
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


		public class ModelWithList
		{
			public ModelWithList()
			{
				this.StringList = new List<string>();
			}

			public int Id { get; set; }
			public List<string> StringList { get; set; }

			public bool Equals(ModelWithList other)
			{
				if (ReferenceEquals(null, other)) return false;
				if (ReferenceEquals(this, other)) return true;
				return other.Id == Id && StringList.EquivalentTo(other.StringList);
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != typeof (ModelWithList)) return false;
				return Equals((ModelWithList) obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return (Id*397) ^ (StringList != null ? StringList.GetHashCode() : 0);
				}
			}
		}

		[Test]
		public void Can_serialize_Model_with_array()
		{
			var model = new ModelWithList
			{
				Id = 1,
				StringList = { "One", "Two", "Three" }
			};

			SerializeAndCompare(model);
		}

		[Test]
		public void Can_serialize_Model_with_array_of_escape_chars()
		{
			var model = new ModelWithList
			{
				Id = 1,
				StringList = { @"1 \ 2 \r 3 \n 4 \b 5 \f 6 """, @"1 \ 2 \r 3 \n 4 \b 5 \f 6 """ }
			};

			SerializeAndCompare(model);
		}

		[Test]
		public void Can_deserialize_json_list_with_whitespace()
		{
			var model = new ModelWithList
			{
				Id = 1,
				StringList = { " One ", " Two " }
			};

			Log(JsonSerializer.SerializeToString(model));

			const string json = "\t { \"Id\" : 1 , \n \"StringList\" \t : \n [ \t \" One \" \t , \t \" Two \" \t ] \n } \t ";

			var fromJson = JsonSerializer.DeserializeFromString<ModelWithList>(json);

			Assert.That(fromJson, Is.EqualTo(model));
		}
	}
}