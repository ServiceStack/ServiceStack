using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests.Text
{
	[TestFixture]
	public class AdhocJsTests
	{
		[Test]
		public void Can_Deserialize()
		{
			var items = TypeSerializer.DeserializeFromString<List<string>>(
				"/CustomPath35/api,/CustomPath40/api,/RootPath35,/RootPath40,:82,:83,:5001/api,:5002/api,:5003,:5004");

			Console.WriteLine(items.Dump());
		}
	}
}