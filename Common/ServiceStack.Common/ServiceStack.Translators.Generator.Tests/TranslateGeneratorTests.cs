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
		private const string outPath = @"C:\Projects\PoToPe\trunk\servicestack\Common\ServiceStack.Common\ServiceStack.Translators.Generator.Tests\Build\out\";

		[Test]
		public void Gen_translators_with_CSharp()
		{
			var generator = new TranslatorClassGenerator(CodeLang.CSharp);
			generator.Write(typeof(Customer), outPath + "Customer.generated.cs");
			generator.Write(typeof(Address), outPath + "Address.generated.cs");
			generator.Write(typeof(PhoneNumber), outPath + "PhoneNumber.generated.cs");
		}

		//[Test]
		//public void Gen_translators_with_Vb()
		//{
		//    var generator = new TranslatorClassGenerator(CodeLang.Vb);
		//    generator.Write(typeof(Customer), outPath + "Customer.generated.vb");
		//    generator.Write(typeof(Address), outPath + "Address.generated.vb");
		//}

		//[Test]
		//public void Gen_translators_with_FSharp()
		//{
		//    var generator = new TranslatorClassGenerator(CodeLang.FSharp);
		//    generator.Write(typeof(Customer), outPath + "Customer.generated.fs");
		//    generator.Write(typeof(Address), outPath + "Address.generated.fs");
		//}

		//[Test]
		//public void Gen_translators_with_JScript()
		//{
		//    var generator = new TranslatorClassGenerator(CodeLang.JScript);
		//    generator.Write(typeof(Customer), outPath + "Customer.generated.js");
		//    generator.Write(typeof(Address), outPath + "Address.generated.js");
		//}

		//[Test]
		//public void Gen_translators_with_Boo()
		//{
		//    var generator = new TranslatorClassGenerator(CodeLang.Boo);
		//    generator.Write(typeof(Customer), outPath + "Customer.generated.boo");
		//    generator.Write(typeof(Address), outPath + "Address.generated.boo");
		//}
	}
}
