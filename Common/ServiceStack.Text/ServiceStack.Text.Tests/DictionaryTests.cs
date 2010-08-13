using System.Collections.Generic;
using NUnit.Framework;

namespace ServiceStack.Text.Tests
{
	[TestFixture]
	public class DictionaryTests
		: TestBase
	{

		[Test]
		public void Can_serialize_one_level_dictionary()
		{
			var map = new Dictionary<string, int>
          	{
				{"One", 1}, {"Two", 2}, {"Three", 3}, 
          	};

			Serialize(map);
		}

		[Test]
		public void Can_serialize_empty_map()
		{
			var emptyMap = new Dictionary<string, int>();

			Serialize(emptyMap);
		}

		[Test]
		public void Can_serialize_empty_string_map()
		{
			var emptyMap = new Dictionary<string, string>();

			Serialize(emptyMap);
		}

		[Test]
		public void Can_serialize_two_level_dictionary()
		{
			var map = new Dictionary<string, Dictionary<string, int>>
          		{
					{"map1", new Dictionary<string, int>
			         	{
							{"One", 1}, {"Two", 2}, {"Three", 3}, 
			         	}
					},
					{"map2", new Dictionary<string, int>
			         	{
							{"Four", 4}, {"Five", 5}, {"Six", 6}, 
			         	}
					},
          		};

			Serialize(map);
		}

		[Test]
		public void Can_serialize_two_level_dictionary_with_int_key()
		{
			var map = new Dictionary<int, Dictionary<string, int>>
          		{
					{1, new Dictionary<string, int>
			         	{
							{"One", 1}, {"Two", 2}, {"Three", 3}, 
			         	}
					},
					{2, new Dictionary<string, int>
			         	{
							{"Four", 4}, {"Five", 5}, {"Six", 6}, 
			         	}
					},
          		};

			Serialize(map);
		}
		
	}
}