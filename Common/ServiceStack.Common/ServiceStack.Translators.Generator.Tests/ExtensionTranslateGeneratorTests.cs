using NUnit.Framework;
using ServiceStack.Translators.Generator.Tests.Support;
using ServiceStack.Translators.Generator.Tests.Support.DataContract;

namespace ServiceStack.Translators.Generator.Tests
{
	[TestFixture]
	public class ExtensionTranslateGeneratorTests
	{
		private const string outPath = @"C:\Projects\code.google\Common\ServiceStack.Common\ServiceStack.Translators.Generator.Tests\Build\out-ext\";

		[Test]
		public void Gen_extension_translators_with_CSharp()
		{
			var generator = new ExtensionTranslatorClassGenerator(CodeLang.CSharp);
			generator.Write(typeof(ServiceModelTranslator), outPath);
		}

		[Test]
		public void Gen_explicit_extension_translators_with_CSharp()
		{
			var generator = new ExtensionTranslatorClassGenerator(CodeLang.CSharp);
			generator.Write(typeof(ExplicitTranslator), outPath);
		}

	}
}
