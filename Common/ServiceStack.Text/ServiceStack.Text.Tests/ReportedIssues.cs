using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ServiceStack.Text.Tests
{
	[TestFixture]
	public class ReportedIssues
		: TestBase
	{

		[Test]
		public void Issue5_Can_serialize_Dictionary_with_null_value()
		{
			var map = new Dictionary<string, string> {
				{"p1","v1"},{"p2","v2"},{"p3",null},
			};

			Serialize(map);
		}

		public abstract class CorrelativeDataBase
		{
			protected CorrelativeDataBase()
			{
				CorrelationIdentifier = GetNextId();
			}

			public Guid CorrelationIdentifier { get; set; }

			protected static Guid GetNextId()
			{
				return Guid.NewGuid();
			}
		}

		public sealed class TestObject : CorrelativeDataBase
		{
			public Type SomeType { get; set; }
			public string SomeString { get; set; }
			public IEnumerable<Type> SomeTypeList { get; set; }
			public IEnumerable<Type> SomeTypeList2 { get; set; }
			public IEnumerable<object> SomeObjectList { get; set; }
		}

		[Test]
		public void Serialize_object_with_type_field()
		{
			var obj = new TestObject
			{
				SomeType = typeof(string),
				SomeString = "Test",
				SomeObjectList = new object[0]
			};

			Serialize(obj);
		}

		[Test]
		public void Serialize_object_with_type_field2()
		{

			var obj = new TestObject
			{
				SomeType = typeof(string),
				SomeString = "Test",
				SomeObjectList = new object[0]
			};

			var strModel = TypeSerializer.SerializeToString<object>(obj);
			Console.WriteLine("Len: " + strModel.Length + ", " + strModel);
			var toModel = TypeSerializer.DeserializeFromString<TestObject>(strModel);
		}

		class Article
		{
			public string title { get; set; }
			public string url { get; set; }
			public string author { get; set; }
			public string author_id { get; set; }
			public string date { get; set; }
			public string type { get; set; }
		}

		[Test]
		public void Serialize_Dictionary_with_backslash_as_last_char()
		{
			var map = new Dictionary<string, Article>
          	{
				{
					"http://www.eurogamer.net/articles/2010-09-14-vanquish-limited-edition-has-7-figurine",
					new Article
					{
						title = "Vanquish Limited Edition has 7\" figurine",
						url = "articles/2010-09-14-vanquish-limited-edition-has-7-figurine",
						author = "Wesley Yin-Poole",
						author_id = "621",
						date = "14/09/2010",
						type = "news",
					}
				},
				{
					"http://www.eurogamer.net/articles/2010-09-14-supercar-challenge-devs-next-detailed",
					new Article
					{
						title = "SuperCar Challenge dev's next detailed",
						url = "articles/2010-09-14-supercar-challenge-devs-next-detailed",
						author = "Wesley Yin-Poole",
						author_id = "621",
						date = "14/09/2010",
						type = "news",
					}
				},
				{
					"http://www.eurogamer.net/articles/2010-09-14-hmv-to-sell-dead-rising-2-a-day-early",
					new Article
					{
						title = "HMV to sell Dead Rising 2 a day early",
						url = "articles/2010-09-14-hmv-to-sell-dead-rising-2-a-day-early",
						author = "Wesley Yin-Poole",
						author_id = "621",
						date = "14/09/2010",
						type = "News",
					}
				},
          	};

			Serialize(map);

			var json = JsonSerializer.SerializeToString(map);
			var fromJson = JsonSerializer.DeserializeFromString<Dictionary<string, Article>>(json);

			Assert.That(fromJson, Has.Count.EqualTo(map.Count));
		}
	}
}