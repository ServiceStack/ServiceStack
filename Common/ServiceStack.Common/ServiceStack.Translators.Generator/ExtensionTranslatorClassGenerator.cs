using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ServiceStack.Logging;
using ServiceStack.Translators.Generator.Filters;

namespace ServiceStack.Translators.Generator
{
	public class ExtensionTranslatorClassGenerator
	{
		private readonly CodeLang codeLang;
		private readonly ICodeGenerator generator;
		private static readonly ILog log = LogManager.GetLogger(typeof(TranslatorClassGenerator));

		public ExtensionTranslatorClassGenerator()
			: this(CodeLang.CSharp)
		{
		}

		public ExtensionTranslatorClassGenerator(CodeLang codeLang)
		{
			this.codeLang = codeLang;
			this.generator = CodeDomUtils.CreateGenerator(codeLang);
		}

		public void Write(Type extensionTranslatorType, string basePathName)
		{
			Write(extensionTranslatorType, basePathName, x => true);
		}

		public void Write(Type extensionTranslatorType, string basePathName, Func<string, bool> doGenerate)
		{
			var attrs = extensionTranslatorType.GetCustomAttributes(typeof(TranslateModelExtensionAttribute), false);
			foreach (var attr in attrs)
			{
				var extAttr = (TranslateModelExtensionAttribute)attr;
				var translateAttr = new TranslateModelAttribute(extAttr.ToType);
				var translatorTypeName = extAttr.FromType.Name + extensionTranslatorType.Name;
				var pathName = basePathName + translatorTypeName + ".cs";

				if (!doGenerate(translatorTypeName)) continue;

				Write(extensionTranslatorType, translatorTypeName, extAttr.FromType, pathName, translateAttr);
			}
		}

		public void Write(Type extensionTranslatorType, string translatorTypeName, Type fromDtoType, string pathName, TranslateModelAttribute attr)
		{
			var sourceBuilder = new StringBuilder();
			using (var writer = new StringWriter(sourceBuilder))
			{
				var options = new CodeGeneratorOptions {
					BracingStyle = "C",
					IndentString = "\t",
				};

				var codeNamespace = new CodeNamespace(extensionTranslatorType.Namespace);

				codeNamespace.Imports.Add(new CodeNamespaceImport("System"));
				codeNamespace.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));

				var declaration = DeclareType(translatorTypeName);

				//var typeDeclaration = new CodeSnippetStatement("public {0}")
				codeNamespace.Types.Add(declaration);

				foreach (var modelType in attr.ForTypes)
				{
					declaration.Members.Add(ToModelMethod(translatorTypeName, fromDtoType, modelType));
					var fromDto = fromDtoType.Param("from", typeof(IEnumerable<>));
					declaration.Members.Add(TranslatorClassGenerator.ToModelListMethod(fromDtoType, modelType, fromDto.ExtensionVar()));

					declaration.Members.Add(UpdateModelMethod(translatorTypeName, fromDtoType, modelType));

					var fromModel = modelType.Param("from");
					declaration.Members.Add(TranslatorClassGenerator.ParseMethod(fromDtoType, modelType, DeclareParseMethod(fromDtoType, fromModel), fromModel));

					var fromModelList = modelType.Param("from", typeof(IEnumerable<>));
					declaration.Members.Add(TranslatorClassGenerator.ParseEnumerableMethod(fromDtoType, modelType, DeclareParseEnumerableMethod(fromDtoType, fromModelList), fromModelList));
				}

				generator.GenerateCodeFromNamespace(codeNamespace, writer, options);
			}

			var codeFilter = CodeFilters.GetCodeFilter(codeLang);
			sourceBuilder = codeFilter.ApplyExtensionFilter(sourceBuilder);
			File.WriteAllText(pathName, sourceBuilder.ToString());
		}

		protected static CodeTypeDeclaration DeclareType(string translatorTypeName)
		{
			var typeDeclaration = new CodeTypeDeclaration {
				IsClass = true,
				IsPartial = true,
				Name = translatorTypeName,
				TypeAttributes = (TypeAttributes.Public | TypeAttributes.Sealed)
			};

			return typeDeclaration;
		}

		protected static CodeMemberMethod DeclareToModelMethod(Type toModelType, string methodName, CodeParameterDeclarationExpression from)
		{
			var method = methodName.DeclareMethod(toModelType, MemberAttributes.Public | MemberAttributes.Static, from);
			return method;
		}

		protected static CodeMemberMethod DeclareToListMethod(Type toModelType, string methodName, CodeParameterDeclarationExpression from)
		{
			from.ExtensionVar();
			return methodName.DeclareMethod(toModelType.RefGeneric(typeof(List<>)),
				MemberAttributes.Public | MemberAttributes.Static, from);
		}

		protected static CodeMemberMethod DeclareParseMethod(Type toDtoType, CodeParameterDeclarationExpression from)
		{
			const string methodName = "Parse";
			return methodName.DeclareMethod(toDtoType, MemberAttributes.Public | MemberAttributes.Static, from.ExtensionVar());
		}

		protected static CodeMemberMethod DeclareParseEnumerableMethod(Type modelType, CodeParameterDeclarationExpression from)
		{
			const string methodName = "ParseAll";
			return methodName.DeclareMethod(
					modelType.RefGeneric(typeof(List<>)), MemberAttributes.Public | MemberAttributes.Static, from.ExtensionVar());
		}

		public static CodeTypeMember ToModelMethod(string translatorTypeName, Type fromDtoType, Type toModelType)
		{
			var methodName = TranslatorClassGenerator.GetToModelMethodName(fromDtoType, toModelType);
			var updateMethodName = TranslatorClassGenerator.GetUpdateMethodName(fromDtoType, toModelType);
			var fromDto = fromDtoType.Param("from");
			var method = DeclareToModelMethod(toModelType, methodName, fromDto.ExtensionVar());
			method.Statements.Add(translatorTypeName.CallStatic(updateMethodName, fromDto.RefArg(), toModelType.New()).Return());
			return method;
		}

		public static CodeTypeMember UpdateModelMethod(string translatorTypeName, Type fromDtoType, Type toModelType)
		{
			var methodName = TranslatorClassGenerator.GetUpdateMethodName(fromDtoType, toModelType);
			var fromDto = fromDtoType.Param("fromModel");
			var toModel = toModelType.Param("model");
			var method = methodName.DeclareMethod(toModelType, MemberAttributes.Public | MemberAttributes.Static, fromDto.ExtensionVar(), toModel);

			var typeNames = toModelType.GetProperties().ToList().Select(x => x.Name);
			var fromDtoVar = fromDtoType.DeclareVar("from", fromDto.RefArg());
			method.Statements.Add(fromDtoVar);
			foreach (var fromDtoProperty in fromDtoType.GetProperties())
			{
				if (!typeNames.Contains(fromDtoProperty.Name)) continue;

				method.Statements.Add(TranslatorClassGenerator.CreateToModelAssignmentMethod(toModel, fromDtoProperty, toModelType, fromDtoVar.RefVar()));
			}

			method.Statements.Add(toModel.Return());
			return method;
		}
	}
}