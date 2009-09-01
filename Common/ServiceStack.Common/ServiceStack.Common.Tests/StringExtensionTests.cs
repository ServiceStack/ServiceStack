using System;
using System.Web;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Common.Extensions;

namespace ServiceStack.Common.Tests
{
	[TestFixture]
	public class StringExtensionTests
	{
		[Test]
		public void To_works_with_ValueTypes()
		{
			Assert.That(1.ToString().To<int>(), Is.EqualTo(1));
		}

		[Test]
		public void To_on_null_or_empty_string_returns_default_value_supplied()
		{
			const string nullString = null;
			Assert.That("".To(1), Is.EqualTo(1));
			Assert.That("".To(default(int)), Is.EqualTo(default(int)));
			Assert.That(nullString.To(1), Is.EqualTo(1));
		}

		[Test]
		public void To_ValueType_on_null_or_empty_string_returns_default_value()
		{
			Assert.That("".To<int>(), Is.EqualTo(default(int)));
		}

		[Test]
		public void To_UrlEncode()
		{
			const string url = "http://www.servicestack.net/a?b=c&d=f";
			var urlEncoded = url.UrlEncode();

			Assert.That(urlEncoded, Is.EqualTo(HttpUtility.UrlEncode(url)));
		}
		[Test]
		public void To_UrlDecode()
		{
			const string url = "http://www.servicestack.net/a?b=c&d=f";
			var urlEncoded = url.UrlEncode();
			var decodedUrl = urlEncoded.UrlDecode();

			Assert.That(decodedUrl, Is.EqualTo(url));
		}

	}
}
