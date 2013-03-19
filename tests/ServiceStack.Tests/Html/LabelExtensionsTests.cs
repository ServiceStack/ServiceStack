using NUnit.Framework;
using ServiceStack.Html;
using ServiceStack.Tests.Html.Support.Types;

namespace ServiceStack.Tests.Html
{
	[TestFixture]
	public class LabelExtensionsTests
	{
		HtmlHelper<Person> html;

		[SetUp]
		public void SetUp()
		{
			html = new HtmlHelper<Person>();
		}

		[Test]
		public void LabelFor_SimpleProperty_ForAttributeIsSameAsName()
		{
			MvcHtmlString result = html.LabelFor(p => p.First);

			Assert.AreEqual(@"<label for=""First"">First</label>", result.ToHtmlString());
		}

		[Test]
		public void LabelFor_NestedProperty_ForAttributeReferencesElementIDWithUnderscores()
		{
			MvcHtmlString result = html.LabelFor(p => p.Home.City);

			Assert.AreEqual(@"<label for=""Home_City"">City</label>", result.ToHtmlString());
		}
	}
}
