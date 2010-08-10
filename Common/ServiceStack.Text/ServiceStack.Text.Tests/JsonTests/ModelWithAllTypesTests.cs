using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Text.Tests.DynamicModels;

namespace ServiceStack.Text.Tests.JsonTests
{
	[TestFixture]
	public class ModelWithAllTypesTests
	{
		[Test]
		public void Can_Serialize()
		{
			var model = ModelWithAllTypes.Create(1);
			var s = JsonSerializer.SerializeToString(model);

			Console.WriteLine(s);
		}

		[Test]
		public void Can_Serialize_list()
		{
			var model = new List<ModelWithAllTypes>
           	{
				ModelWithAllTypes.Create(1),
				ModelWithAllTypes.Create(2)
           	};
			var s = JsonSerializer.SerializeToString(model);

			Console.WriteLine(s);
		}

		[Test]
		public void Can_Serialize_map()
		{
			var model = new Dictionary<string, ModelWithAllTypes>
           	{
				{"A", ModelWithAllTypes.Create(1)},
				{"B", ModelWithAllTypes.Create(2)},
           	};
			var s = JsonSerializer.SerializeToString(model);

			Console.WriteLine(s);
		}


	}
}