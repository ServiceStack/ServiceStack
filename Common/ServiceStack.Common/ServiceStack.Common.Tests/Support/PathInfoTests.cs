using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace ServiceStack.Common.Tests.Support
{
	[TestFixture]
	public class PathInfoTests
	{
		[Test]
		public void Test_with_full_path_uri()
		{
			var pathInfo = PathInfo.Parse("Controller://Action/arg1/arg2?Name=foo&Age=10");

			Assert.That(pathInfo.ControllerName, Is.EqualTo("Controller"));
			Assert.That(pathInfo.ActionName, Is.EqualTo("Action"));
			Assert.That(pathInfo.Arguments.Count, Is.EqualTo(2));
			Assert.That(pathInfo.Options.Count, Is.EqualTo(2));
		}

		[Test]
		public void Test_with_controller_and_pathUri()
		{
			var pathInfo = PathInfo.Parse("Controller://Action/arg1");

			Assert.That(pathInfo.ControllerName, Is.EqualTo("Controller"));
			Assert.That(pathInfo.ActionName, Is.EqualTo("Action"));
			Assert.That(pathInfo.Arguments.Count, Is.EqualTo(1));
		}

	}
}