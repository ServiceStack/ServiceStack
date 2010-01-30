using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace ServiceStack.Common.Tests.Models
{
	public class StringFactory
		: ModelFactoryBase<string>
	{
		readonly string[] StringValues = new[] {
			"one", "two", "three", "four", 
			"five", "six", "seven"
		};

		public override void AssertIsEqual(string actual, string expected)
		{
			Assert.That(actual, Is.EqualTo(expected));
		}

		public override string CreateInstance(int i)
		{
			return i < StringValues.Length
				? StringValues[i]
				: i.ToString();
		}
	}
}