using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Reflection;
using Microsoft.CSharp;
using Microsoft.VisualBasic;

namespace ServiceStack.Translators.Generator
{
	public class CodeDomUtils
	{
		public static void GenerateCSharpCode(CodeCompileUnit ccu, string outFilePath)
		{

			var cp = new CompilerParameters();

			var codeProvider = new CSharpCodeProvider();
			using (var writer = new IndentedTextWriter(new StreamWriter(outFilePath, false), "\t"))
			{
				codeProvider.GenerateCodeFromCompileUnit(ccu, writer, new CodeGeneratorOptions());
				var generator = codeProvider.CreateGenerator(writer);
				var options = new CodeGeneratorOptions();

				generator.GenerateCodeFromCompileUnit(new CodeSnippetCompileUnit("using System"), writer, options);
				var declaration = new CodeTypeDeclaration
				                  	{
				                  		IsClass = true,
				                  		Name = "Briefcase",
				                  		TypeAttributes = TypeAttributes.Public
				                  	};
			}

			//cp.GenerateExecutable = false;
			//cp.OutputAssembly = "CSharpSample.exe";
			//cp.GenerateInMemory = false;
			var compilerResults = codeProvider.CompileAssemblyFromDom(cp, ccu);
		}
	}
}