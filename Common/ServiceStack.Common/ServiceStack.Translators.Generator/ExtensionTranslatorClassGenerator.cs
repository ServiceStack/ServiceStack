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
using ServiceStack.Translators.Generator.Support;

namespace ServiceStack.Translators.Generator
{
	public class ExtensionTranslatorClassGenerator
	{
		private readonly CodeLang codeLang;
		private readonly ICodeGenerator generator;
		private static readonly ILog log = LogManager.GetLogger(typeof(TranslatorClassGenerator));
		private Dictionary<Type, Dictionary<Type, TranslateAttribute>> sourceTypeTranslateAttributeMap;

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
			var attrs = extensionTranslatorType.GetCustomAttributes(typeof(TranslateExtensionAttribute), false);
			sourceTypeTranslateAttributeMap = new Dictionary<Type, Dictionary<Type, TranslateAttribute>>();

			//Build the dictionary prior to generating so we can know about the details of all the other model translators.
			var extAttrs = new List<TranslateExtensionAttribute>();
			foreach (var attr in attrs)
			{
				var extAttr = (TranslateExtensionAttribute)attr;
				if (!this.sourceTypeTranslateAttributeMap.ContainsKey(extAttr.SourceType))
				{
					this.sourceTypeTranslateAttributeMap[extAttr.SourceType] = new Dictionary<Type, TranslateAttribute>();	
				}
				this.sourceTypeTranslateAttributeMap[extAttr.SourceType][extAttr.TargetType] = extAttr;
				extAttrs.Add(extAttr);
			}

			foreach (var extAttr in extAttrs)
			{
				var translatorTypeName = extAttr.SourceType.Name + extensionTranslatorType.Name;
				var pathName = basePathName + translatorTypeName + ".cs";

				if (!doGenerate(translatorTypeName)) continue;

				Write(extensionTranslatorType, translatorTypeName, extAttr.SourceType, pathName, extAttr);
			}
		}

		public void Write(Type extensionTranslatorType, string translatorTypeName, Type fromSourceType, string pathName, TranslateAttribute attr)
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

				codeNamespace.Types.Add(declaration);

				declaration.Members.Add(ToTargetMethod(attr, translatorTypeName));

				declaration.Members.Add(ConvertToTargetsMethod(attr));

				declaration.Members.Add(UpdateTargetMethod(attr, translatorTypeName));

				declaration.Members.Add(ConvertToSourceMethod(attr));

				declaration.Members.Add(ConvertToSourcesMethod(attr));

				generator.GenerateCodeFromNamespace(codeNamespace, writer, options);
			}

			var codeFilter = CodeFilters.GetCodeFilter(codeLang);
			sourceBuilder = codeFilter.ApplyExtensionFilter(sourceBuilder);
			File.WriteAllText(pathName, sourceBuilder.ToString());
		}

		private static CodeTypeDeclaration DeclareType(string translatorTypeName)
		{
			var typeDeclaration = new CodeTypeDeclaration {
				IsClass = true,
				IsPartial = true,
				Name = translatorTypeName,
				TypeAttributes = (TypeAttributes.Public | TypeAttributes.Sealed)
			};

			return typeDeclaration;
		}

		private static CodeMemberMethod DeclareConvertToTargetMethod(Type toTargetType, string methodName, CodeParameterDeclarationExpression from)
		{
			var method = methodName.DeclareMethod(toTargetType, MemberAttributes.Public | MemberAttributes.Static, from);
			return method;
		}

		private static CodeTypeMember ToTargetMethod(TranslateAttribute attr, string translatorTypeName)
		{
			var methodName = attr.GetConvertToTargetMethodName();
			var updateMethodName = attr.GetUpdateTargetMethodName();
			var fromSource = attr.SourceType.Param("from");
			var method = DeclareConvertToTargetMethod(attr.TargetType, methodName, fromSource.ExtensionVar());
			method.Statements.Add(translatorTypeName.CallStatic(updateMethodName, fromSource.RefArg(), attr.TargetType.New()).Return());
			return method;
		}

		private CodeTypeMember UpdateTargetMethod(TranslateAttribute attr, string translatorTypeName)
		{
			var methodName = attr.GetUpdateTargetMethodName();
			var fromSource = attr.SourceType.Param("fromParam");
			var toTarget = attr.TargetType.Param("to");
			var method = methodName.DeclareMethod(attr.TargetType, MemberAttributes.Public | MemberAttributes.Static, fromSource.ExtensionVar(), toTarget);

			var typeNames = attr.TargetType.GetProperties().ToList().Select(x => x.Name);
			var fromSourceVar = attr.SourceType.DeclareVar("from", fromSource.RefArg());
			method.Statements.Add(fromSourceVar);
			foreach (var fromSourceProperty in attr.SourceType.GetProperties())
			{
				if (!typeNames.Contains(fromSourceProperty.Name)) continue;

				method.Statements.Add(TranslatorClassGenerator.CreateToTargetAssignmentMethod(attr, toTarget, fromSourceProperty, fromSourceVar.RefVar(), GetTypesTranslateAttributeFn));
			}

			method.Statements.Add(toTarget.Return());
			return method;
		}

		/// <summary>
		/// Provides the functionality to retrieve the [TranslateAttribute] for the matching source and target types
		/// </summary>
		/// <param name="sourceType">Type sourceType.</param>
		/// <param name="targetType">The targetType.</param>
		/// <returns></returns>
		private TranslateAttribute GetTypesTranslateAttributeFn(Type sourceType, Type targetType)
		{
			log.DebugFormat("GetTypesTranslateAttributeFn: {0} => {1}", sourceType.Name, targetType.Name);
			Dictionary<Type, TranslateAttribute> targetAttributeMap; 
			if (this.sourceTypeTranslateAttributeMap.TryGetValue(sourceType, out targetAttributeMap))
			{
				TranslateAttribute attr;
				if (targetAttributeMap.TryGetValue(targetType, out attr))
				{
					log.Debug("found matching attr");
					return attr;
				}
			}
			log.Debug("could not find matching attr");
			return null;
		}

		private CodeTypeMember ConvertToSourceMethod(TranslateAttribute attr)
		{
			var from = attr.TargetType.Param("from");
			var method = TranslatorClassGenerator.DeclareToSourceMethod(attr, from.ExtensionVar());
			log.DebugFormat("ConvertToSourceMethod: {0}", attr.ToFormatString());
			return TranslatorClassGenerator.ConvertToSourceMethod(attr, method, from, GetTypesTranslateAttributeFn);
		}

		private CodeTypeMember ConvertToSourcesMethod(TranslateAttribute attr)
		{
			var from = attr.TargetType.Param("from", typeof(IEnumerable<>));
			var method = TranslatorClassGenerator.DeclareToSourcesMethod(attr, from.ExtensionVar());
			log.DebugFormat("ConvertToSourcesMethod: {0}", attr.ToFormatString());
			return TranslatorClassGenerator.ConvertToSourcesMethod(attr, method, from);
		}

		public static CodeMemberMethod ConvertToTargetsMethod(TranslateAttribute attr)
		{
			var from = attr.SourceType.Param("from", typeof(IEnumerable<>));
			return TranslatorClassGenerator.ConvertToTargetsMethod(attr, from.ExtensionVar());
		}

	}
}