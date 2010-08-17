using System;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;

namespace ServiceStack.Text.Tests.JsonTests
{
	[TestFixture]
	public class BasicJsonTests
		: TestBase
	{
		public class JsonPrimitives
		{
			public int Int { get; set; }
			public long Long { get; set; }
			public float Float { get; set; }
			public double Double { get; set; }
			public bool Boolean { get; set; }
			public DateTime DateTime { get; set; }
			public string NullString { get; set; }

			public static JsonPrimitives Create(int i)
			{
				return new JsonPrimitives
				{
					Int = i,
					Long = i,
					Float = i,
					Double = i,
					Boolean = i % 2 == 0,
					DateTime = new DateTime(DateTimeExtensions.UnixEpoch + (i * DateTimeExtensions.TicksPerMs)),
				};
			}
		}

		[Test]
		public void Can_handle_json_primitives()
		{
			var json = JsonSerializer.SerializeToString(JsonPrimitives.Create(1));
			Log(json);

			Assert.That(json, Is.EqualTo(
				"{\"Int\":1,\"Long\":1,\"Float\":1,\"Double\":1,\"Boolean\":false,\"DateTime\":\"\\/Date(1+0000)\\/\"}"));
		}

		[Test]
		public void Can_parse_json_with_nulls()
		{
			const string json = "{\"Int\":1,\"NullString\":null}";
			var value = JsonSerializer.DeserializeFromString<JsonPrimitives>(json);

			Assert.That(value.Int, Is.EqualTo(1));
			Assert.That(value.NullString, Is.Null);
		}
	}
}