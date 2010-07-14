using System;
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
	}
}