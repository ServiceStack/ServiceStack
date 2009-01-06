using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ServiceStack.Translators.Generator.Tests.Support.DataContract;

namespace ServiceStack.Translators.Generator.Tests
{
	[TestFixture]
	public class TranslateGeneratorTests
	{
		private const string outPath = @"C:\Projects\code.google\Common\ServiceStack.Common\ServiceStack.Translators.Generator.Tests\Build\out\";
		
		[Test]
		public void Test()
		{
			TranslatorClassGenerator.Write(typeof(Customer), outPath + "Customer.generated.cs");
			TranslatorClassGenerator.Write(typeof(Address), outPath + "Address.generated.cs");
		}
	}
}
