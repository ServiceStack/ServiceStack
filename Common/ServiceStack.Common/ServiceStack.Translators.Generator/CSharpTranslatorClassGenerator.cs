using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CSharp;
using ServiceStack.Logging;

namespace ServiceStack.Translators.Generator
{
	public class CSharpTranslatorClassGenerator
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(CSharpTranslatorClassGenerator));

		public static void Write(Type modelType, string pathName)
		{
			var attr =(TranslateModelAttribute)modelType.GetCustomAttributes(typeof(TranslateModelAttribute), false).GetValue(0);
			Write(modelType, pathName, attr);
		}

		public static void Write(Type modelType, string pathName, TranslateModelAttribute attr)
		{
			using (var writer = new StreamWriter(pathName, false))
			{
				var codeProvider = new CSharpCodeProvider();
				var generator = codeProvider.CreateGenerator(writer);
				var options = new CodeGeneratorOptions {
					BracingStyle = "C",
					IndentString = "    ",
				};

				var codeNamespace = new CodeNamespace(modelType.Namespace);

				codeNamespace.Imports.Add(new CodeNamespaceImport("System"));
				codeNamespace.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));

				var declaration = new CodeTypeDeclaration {
					IsClass = true,
					Name = modelType.Name,
					IsPartial = true,
					TypeAttributes = TypeAttributes.Public,
				};
				codeNamespace.Types.Add(declaration);

				foreach (var type in attr.ForTypes)
				{
					var attrs = type.GetCustomAttributes(typeof(TranslateModelAttribute), false).ToList();
					declaration.Members.Add(CreateToModelMethod(modelType, type));
					declaration.Members.Add(CreateParseMethod(modelType, type));
					declaration.Members.Add(CreateParseEnumerableMethod(modelType, type));
				}
				generator.GenerateCodeFromNamespace(codeNamespace, writer, options);

			}
		}

		private static CodeMemberMethod CreateToModelMethod(Type modelType, Type type)
		{
			var methodName = type.Name == modelType.Name ? "ToModel" : "To" + type.Name;
			var method = new CodeMemberMethod {
				Name = methodName,
				ReturnType = new CodeTypeReference(type.FullName),
				Attributes = MemberAttributes.Public,
			};

			var indent = "\t\t\t";
			method.Statements.Add(new CodeSnippetStatement(indent + "var model = new " + type.FullName + " {"));
			var typeNames = type.GetProperties().ToList().Select(x => x.Name);
			foreach (var property in modelType.GetProperties())
			{
				if (!typeNames.Contains(property.Name)) continue;

				var isModelAlso = property.PropertyType.GetCustomAttributes(typeof(TranslateModelAttribute), false).Count() > 0;
				if (isModelAlso)
				{
					method.Statements.Add(new CodeSnippetStatement(string.Format("\t{0}{1} = this.{1}.ToModel(),", indent, property.Name)));
				}
				else
				{
					method.Statements.Add(new CodeSnippetStatement(string.Format("\t{0}{1} = this.{1},", indent, property.Name)));
				}
			}
			method.Statements.Add(new CodeSnippetStatement(indent + "};"));

			method.Statements.Add(new CodeSnippetStatement(indent + "return model;"));
			return method;
		}

		private static CodeMemberMethod CreateParseMethod(Type modelType, Type type)
		{
			var methodName = "Parse";
			var method = new CodeMemberMethod {
				Name = methodName,
				ReturnType = new CodeTypeReference(modelType.Name),
				Attributes = MemberAttributes.Public,
			};
			method.Parameters.Add(new CodeParameterDeclarationExpression(type, "from"));

			var indent = "\t\t\t";

			method.Statements.Add(new CodeSnippetStatement(indent + "var to = new " + modelType.Name + " {"));
			var typeNames = type.GetProperties().ToList().Select(x => x.Name);
			foreach (var property in modelType.GetProperties())
			{
				if (!typeNames.Contains(property.Name)) continue;

				var isModelAlso = property.PropertyType.GetCustomAttributes(typeof(TranslateModelAttribute), false).Count() > 0;
				if (isModelAlso)
				{
					method.Statements.Add(new CodeSnippetStatement(string.Format("\t{0}{1} = new {2}().Parse(from.{1}),", indent, property.Name, property.PropertyType.Name)));
				}
				else
				{
					method.Statements.Add(new CodeSnippetStatement(string.Format("\t{0}{1} = from.{1},", indent, property.Name)));
				}
			}
			method.Statements.Add(new CodeSnippetStatement(indent + "};"));

			method.Statements.Add(new CodeSnippetStatement(indent + "return to;"));
			return method;
		}

		private static CodeMemberMethod CreateParseEnumerableMethod(Type modelType, Type type)
		{
			var methodName = "ParseAll";
			var method = new CodeMemberMethod {
				Name = methodName,
				ReturnType = new CodeTypeReference("List", new CodeTypeReference(modelType.Name)),
				Attributes = MemberAttributes.Public | MemberAttributes.Static,
			};
			method.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("IEnumerable", new CodeTypeReference(type)), "from"));

			var indent = "\t\t\t";

			method.Statements.Add(new CodeSnippetStatement(indent + "var to = new List<" + modelType.Name + ">();"));
			method.Statements.Add(new CodeSnippetStatement(indent + "foreach (var item in from)"));
			method.Statements.Add(new CodeSnippetStatement(indent + "{"));
			method.Statements.Add(new CodeSnippetStatement(string.Format("\t{0}to.Add(new {1}().Parse(item));", indent, modelType.Name)));
			method.Statements.Add(new CodeSnippetStatement(indent + "}"));

			method.Statements.Add(new CodeSnippetStatement(indent + "return to;"));
			return method;
		}
	}
}