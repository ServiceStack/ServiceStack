using ServiceStack.Common.Services.Resources;
using ServiceStack.Logging.Log4Net;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace ServiceStack.Common.Services.Tests.Resources
{
	[TestFixture]
	public class StringResourceManagerTests
	{
		private StringResourceManager StringResources { get; set; }

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			this.StringResources = new StringResourceManager(new Log4NetFactory(true));
		}

		[TestFixtureTearDown]
		public void TestFixtureTearDown()
		{
			this.StringResources = null;
		}

		[Test]
		public void LoadResourcesFromTextFileTest()
		{
			var path = "StringResources.txt";
			this.StringResources.LoadTextFile(path, '\t');

			AssertErrorStringValid(0, "Success");
			AssertErrorStringValid(1, "Invalid user name or password");
			AssertErrorStringValid(2, "Multiple tabs");
			AssertErrorStringInvalid(3);
			AssertErrorStringValid(4, "Invalid signature");

			this.StringResources.Errors.DefaultResource = "foo";
			AssertErrorStringInvalid(123);

			this.StringResources.Errors.DefaultResource = string.Empty;
			AssertErrorStringInvalid(123);
		}

		private void AssertErrorStringValid(int key, string value)
		{
			Assert.That(this.StringResources.Errors.Contains(key), Is.True);
			Assert.That(this.StringResources.Errors[key], Is.EqualTo(value));
		}

		private void AssertErrorStringInvalid(int key)
		{
			Assert.That(this.StringResources.Errors.Contains(key), Is.False);
			Assert.That(this.StringResources.Errors[key], Is.EqualTo(this.StringResources.Errors.DefaultResource));
		}
	}
}