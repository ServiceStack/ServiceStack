using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Reflection;

namespace ServiceStack.Translators.Generator
{
	public class ExtensionTranslatorClassGenerator : TranslatorClassGenerator
	{
		public ExtensionTranslatorClassGenerator()
		{ }

		public ExtensionTranslatorClassGenerator(CodeLang lang)
			: base(lang)
		{ }

		protected override CodeTypeDeclaration DeclareType(Type modelType)
		{
			var typeDeclaration = new CodeTypeDeclaration {
				IsClass = true,
				IsPartial = true,
				Name = modelType.Name,
				TypeAttributes = (TypeAttributes.Public | TypeAttributes.Sealed)
			};

			typeDeclaration.Members.Add(new CodeConstructor { Attributes = MemberAttributes.Private });

			return typeDeclaration;
		}

		protected override CodeMemberMethod DeclareToListMethod(Type toModelType, string methodName, CodeParameterDeclarationExpression from)
		{
			return methodName.DeclareMethod(toModelType.RefGeneric(typeof(List<>)),
				MemberAttributes.Public | MemberAttributes.Static, from);
		}

		protected override CodeMemberMethod DeclareToModelMethod(Type toModelType, string methodName)
		{
			return methodName.DeclareMethod(toModelType, MemberAttributes.Public);
		}

		protected override CodeMemberMethod DeclareParseMethod(string methodName, Type toDtoType, CodeParameterDeclarationExpression from)
		{
			return methodName.DeclareMethod(toDtoType, MemberAttributes.Public | MemberAttributes.Static, from);
		}

		protected override CodeMemberMethod DeclareParseEnumerableMethod(string methodName, Type modelType, CodeParameterDeclarationExpression from)
		{
			return methodName.DeclareMethod(
					modelType.RefGeneric(typeof(List<>)), MemberAttributes.Public | MemberAttributes.Static, from);
		}

	}
}