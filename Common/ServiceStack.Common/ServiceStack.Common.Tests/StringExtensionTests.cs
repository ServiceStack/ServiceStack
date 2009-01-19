using System;
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
		public void To_on_null_or_empty_string_throws_exception()
		{
			try
			{
				Assert.That("".To<int>(), Is.EqualTo(default(int)));
			}
			catch (Exception ex)
			{
				return;
			}
			Assert.Fail("Should've thrown an exception");
		}

	}
}
