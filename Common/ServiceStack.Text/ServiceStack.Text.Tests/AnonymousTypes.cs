using NUnit.Framework;

namespace ServiceStack.Text.Tests
{
	[TestFixture]
	public class AnonymousTypes
		: TestBase
	{
		[Test]
		public void Can_serialize_anonymous_types()
		{
			Serialize(new { Id = 1, Name = "Name", IntList = new[] { 1, 2, 3 } });
		}
	}

}