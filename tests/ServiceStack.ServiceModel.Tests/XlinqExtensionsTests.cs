using System;
using System.Xml.Linq;
using NUnit.Framework;
using ServiceStack.Common;

namespace ServiceStack.ServiceModel.Tests
{
	[TestFixture]
	public class XlinqExtensionsTests
	{
		private const int IntValue = 2147483647;

		private static XElement CreateChildElement<T>(T value)
		{
			return new XElement("el", new XElement("child", value));
		}

		private static XElement CreateEmptyChildElement()
		{
			return new XElement("el", new XElement("child"));
		}

		[Test]
		public void xelement_get_int_test()
		{
			var el = CreateChildElement(IntValue);
			Assert.AreEqual(IntValue, el.GetInt("child"));
		}

		[Test]
		public void xelement_get_int_null_throws_exception_test()
		{
			Assert.Throws<ArgumentNullException>(() => {
				var el = CreateEmptyChildElement();
				el.GetInt("child");
			});
		}

		[Test]
		public void xelement_get_int_text_throws_exception_test()
		{
			Assert.Throws<FormatException>(() => {
				var el = CreateChildElement("Non int value");
				el.GetInt("child");
			});
		}

		[Test]
		public void xelement_get_int_or_default_test()
		{
			var el = CreateChildElement(IntValue);
			Assert.AreEqual(IntValue, el.GetIntOrDefault("child"));

			el = CreateEmptyChildElement();
			Assert.AreEqual(default(int), el.GetIntOrDefault("child"));
		}

		[Test]
		public void xelement_get_nullable_int_test()
		{
			var el = CreateChildElement(IntValue);
			Assert.AreEqual(IntValue, el.GetNullableInt("child").Value);

			el = CreateChildElement((int?)null);
			Assert.AreEqual(null, el.GetNullableInt("child"));
		}
	}
}