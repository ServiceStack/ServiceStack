using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CSharp;
using ServiceStack.Logging;

namespace ServiceStack.Translators.Generator
{
	public class TranslatorClassGenerator
	{
		private readonly ICodeGenerator generator;
		private CodeLang lang;
		public TranslatorClassGenerator() : this(CodeLang.CSharp)
		{
		}

		public TranslatorClassGenerator(CodeLang lang)
		{
			this.lang = lang;
			this.generator = CodeDomUtils.CreateGenerator(lang);
		}

		private static readonly ILog log = LogManager.GetLogger(typeof(TranslatorClassGenerator));

		public void Write(Type modelType, string pathName)
		{
			var attr =(TranslateModelAttribute)modelType.GetCustomAttributes(typeof(TranslateModelAttribute), false).GetValue(0);
			Write(modelType, pathName, attr);
		}

		public void Write(Type modelType, string pathName, TranslateModelAttribute attr)
		{
			using (var writer = new StreamWriter(pathName, false))
			{
				var options = new CodeGeneratorOptions {
					BracingStyle = "C",
					IndentString = "\t",
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
					declaration.Members.Add(ToModelMethod(modelType, type));
					declaration.Members.Add(UpdateModelMethod(modelType, type));
					declaration.Members.Add(ParseMethod(modelType, type));
					declaration.Members.Add(ParseEnumerableMethod(modelType, type));
				}
				generator.GenerateCodeFromNamespace(codeNamespace, writer, options);

			}
		}

		private static CodeMemberMethod ToModelMethod(Type modelType, Type type)
		{
			var methodName = type.Name == modelType.Name ? "ToModel" : "To" + type.Name;
			var method = methodName.DeclareMethod(type, MemberAttributes.Public);
			method.Statements.Add("UpdateModel".Call(type.New()).Return());
			return method;
		}

		private static CodeMemberMethod UpdateModelMethod(Type modelType, Type type)
		{
			var methodName = type.Name == modelType.Name ? "UpdateModel" : "Update" + type.Name;
			var model = type.Param("model");
			var method = methodName.DeclareMethod(type, MemberAttributes.Public, model);

			var typeNames = type.GetProperties().ToList().Select(x => x.Name);
			foreach (var property in modelType.GetProperties())
			{
				if (!typeNames.Contains(property.Name)) continue;

				var isModelAlso = property.PropertyType.GetCustomAttributes(typeof(TranslateModelAttribute), false).Count() > 0;
				if (isModelAlso)
				{
					method.Statements.Add(model.Assign(property.Name, property.Name.ThisProperty().Call("ToModel")));
				}
				else
				{
					method.Statements.Add(model.Assign(property.Name, property.Name));
				}
			}

			method.Statements.Add(model.Return());
			return method;
		}

		private static CodeMemberMethod ParseMethod(Type modelType, Type type)
		{
			var methodName = "Parse";
			var from = type.Param("from");
			var method = methodName.DeclareMethod(modelType, MemberAttributes.Public | MemberAttributes.Static, from);

			// modelType to = new T();
			var to = modelType.DeclareVar("to");
			method.Statements.Add(to);

			var typeNames = type.GetProperties().ToList().Select(x => x.Name);
			foreach (var property in modelType.GetProperties())
			{
				if (!typeNames.Contains(property.Name)) continue;

				var isModelAlso = property.PropertyType.GetCustomAttributes(typeof(TranslateModelAttribute), false).Count() > 0;
				if (isModelAlso)
				{					
					//to[property.Name] = from[property.PropertyType.Name].ToModel() e.g:
					//	to.Address = from.Address.ToModel();

					method.Statements.Add(to.Assign(property.Name, property.PropertyType.Call("Parse", from.RefProperty(property.Name))));
				}
				else
				{
					//to[property.Name] = this[property.Name] e.g:
					//	to.Name = from.Name;
					method.Statements.Add(to.Assign(to.RefProperty(property.Name), from.Name.RefArgument().RefProperty(property.Name)));
				}
			}

			method.Statements.Add(to.Return());
			return method;
		}

		private static CodeMemberMethod ParseEnumerableMethod(Type modelType, Type type)
		{
			var methodName = "ParseAll";
			//var from = new CodeParameterDeclarationExpression(new CodeTypeReference("IEnumerable", new CodeTypeReference(type)), "from");
			var from = type.Param("from", typeof(IEnumerable<>));

			// public static List<modelType> methodName(IEnumerable<modelType> from) e.g:
			//		public static List<T> ParseAll(IEnumerable<T> from)
			var method = methodName.DeclareMethod(
				modelType.RefGeneric(typeof(List<>)), MemberAttributes.Public | MemberAttributes.Static, from);


			var to = "to".DeclareGenericVar(modelType, typeof(List<>));
			method.Statements.Add(to);

			CodeVariableDeclarationStatement item;
			var iter = from.ForEach(type, out item);			
			method.Statements.Add(iter);
			iter.Statements.Add(to.Call("Add", modelType.Call("Parse", item)));

			method.Statements.Add(to.Return());

			return method;
		}
	}
}