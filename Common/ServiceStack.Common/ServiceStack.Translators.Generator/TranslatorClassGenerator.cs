using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ServiceStack.Common.Utils;
using ServiceStack.Logging;
using ServiceStack.Translators.Generator.Support;

namespace ServiceStack.Translators.Generator
{
	public class TranslatorClassGenerator
	{
		private const string LIST_ADD_METHOD = "Add";
		private const string CONVERTER_PARSE_METHOD = "Parse";
		private readonly ICodeGenerator generator;
		private static readonly ILog log = LogManager.GetLogger(typeof(TranslatorClassGenerator));

		public TranslatorClassGenerator()
			: this(CodeLang.CSharp)
		{
		}

		public TranslatorClassGenerator(CodeLang lang)
		{
			this.generator = CodeDomUtils.CreateGenerator(lang);
		}

		//model.PhoneNumbers = this.PhoneNumbers.ConvertAll(delegate(PhoneNumber x) { return x.ToTarget(); });
		public void Write(Type sourceType, string pathName)
		{
			var attr =(TranslateAttribute)sourceType.GetCustomAttributes(typeof(TranslateAttribute), false).GetValue(0);

			//When using [TranslateAttribute] the sourceType is the type that the attribute is decorated on
			//This needs to be set at runtime as its not set at declaration
			attr.SourceType = sourceType;
			Write(attr, pathName);
		}

		public void Write(TranslateAttribute attr, string pathName)
		{
			using (var writer = new StreamWriter(pathName, false))
			{
				var options = new CodeGeneratorOptions {
					BracingStyle = "C",
					IndentString = "\t",
				};

				var codeNamespace = new CodeNamespace(attr.SourceType.Namespace);

				codeNamespace.Imports.Add(new CodeNamespaceImport("System"));
				codeNamespace.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));

				var declaration = DeclareType(attr.SourceType);
				codeNamespace.Types.Add(declaration);

				declaration.Members.Add(ConvertToTargetMethod(attr));
				declaration.Members.Add(ConvertToTargetsMethod(attr));
				declaration.Members.Add(UpdateTargetMethod(attr));
				declaration.Members.Add(ConvertToSourceMethod(attr));
				declaration.Members.Add(ConvertToSourcesMethod(attr));

				generator.GenerateCodeFromNamespace(codeNamespace, writer, options);

			}
		}

		#region Overridable Implementations
		public static CodeTypeDeclaration DeclareType(Type targetType)
		{
			return new CodeTypeDeclaration {
				IsClass = true,
				Name = targetType.Name,
				IsPartial = true,
				TypeAttributes = TypeAttributes.Public,
			};
		}

		public static CodeMemberMethod DeclareToTargetsMethod(TranslateAttribute attr, CodeParameterDeclarationExpression from)
		{
			return attr.GetConvertToTargetsMethodName().DeclareMethod(attr.TargetType.RefGeneric(typeof(List<>)),
				MemberAttributes.Public | MemberAttributes.Static, from);
		}

		public static CodeMemberMethod DeclareToTargetMethod(TranslateAttribute attr)
		{
			return attr.GetConvertToTargetMethodName().DeclareMethod(attr.TargetType, MemberAttributes.Public);
		}

		public static CodeMemberMethod DeclareToSourceMethod(TranslateAttribute attr, CodeParameterDeclarationExpression from)
		{
			return attr.GetConvertToSourceMethodName().DeclareMethod(
				attr.SourceType, MemberAttributes.Public | MemberAttributes.Static, from);
		}

		public static CodeMemberMethod DeclareToSourcesMethod(TranslateAttribute attr, CodeParameterDeclarationExpression from)
		{
			return attr.GetConvertToSourcesMethodName().DeclareMethod(
					attr.SourceType.RefGeneric(typeof(List<>)), MemberAttributes.Public | MemberAttributes.Static, from);
		}
		#endregion

		/*
			public static List<Target.PhoneNumber> ToTargets(List<SourceType.PhoneNumber> sourceCustomers)
			{
				var to = new List<Target.PhoneNumber>();
				foreach (var sourceCustomer in sourceCustomers)
				{
					to.Add(sourceCustomer.ToTarget());
				}
				return to;
			}
		*/
		public static CodeMemberMethod ConvertToTargetsMethod(TranslateAttribute attr)
		{
			var from = attr.SourceType.Param("from", typeof(IEnumerable<>));
			return ConvertToTargetsMethod(attr, from);
		}

		public static CodeMemberMethod ConvertToTargetsMethod(TranslateAttribute attr, CodeParameterDeclarationExpression from)
		{
			var method = DeclareToTargetsMethod(attr, from);

			method.Statements.Add(from.ReturnNullIfNull());

			var to = "to".DeclareGenericVar(attr.TargetType, typeof(List<>));
			method.Statements.Add(to);

			CodeVariableDeclarationStatement item;
			var iter = from.ForEach(attr.SourceType, out item);
			method.Statements.Add(iter);
			var toTargetMethodName = attr.GetConvertToTargetMethodName();
			iter.Statements.Add(item.IfIsNotNull(to.Call(LIST_ADD_METHOD, item.Call(toTargetMethodName))));

			method.Statements.Add(to.Return());

			return method;
		}

		public static CodeTypeMember ConvertToTargetMethod(TranslateAttribute attr)
		{
			var updateMethodName = attr.GetUpdateTargetMethodName();
			var method = DeclareToTargetMethod(attr);
			method.Statements.Add(updateMethodName.Call(attr.TargetType.New()).Return());
			return method;
		}

		public CodeTypeMember UpdateTargetMethod(TranslateAttribute attr)
		{
			return UpdateTargetMethod(attr, GetTypesTranslateAttributeFn);
		}


		/// <summary>
		/// Provides the functionality to retrieve the [TranslateAttribute] for the matching source and target types
		/// </summary>
		/// <param name="sourceType">Type sourceType.</param>
		/// <param name="targetType">The targetType.</param>
		/// <returns></returns>
		private static TranslateAttribute GetTypesTranslateAttributeFn(Type sourceType, Type targetType)
		{
			var attrs = sourceType.GetCustomAttributes(typeof(TranslateAttribute), false).ToList();
			foreach (var oAttr in attrs)
			{
				var attr = (TranslateAttribute)oAttr;
				if (attr.TargetType == targetType)
				{
					//We need to set this as it is not set at the compile time declaration
					attr.SourceType = sourceType;
					return attr;
				}
			}
			return null;
		}

		public static CodeTypeMember UpdateTargetMethod(TranslateAttribute attr, Func<Type, Type, TranslateAttribute> getTypesTranslateAttributeFn)
		{
			var methodName = attr.GetUpdateTargetMethodName();
			var toTarget = attr.TargetType.Param("model");
			var method = methodName.DeclareMethod(attr.TargetType, MemberAttributes.Public, toTarget);

			var typeNames = attr.TargetType.GetProperties().ToList().Select(x => x.Name);
			var fromSourceVar = attr.SourceType.DeclareVar("from", new CodeThisReferenceExpression());
			method.Statements.Add(fromSourceVar);
			foreach (var sourceProperty in attr.SourceType.GetProperties())
			{
				if (!typeNames.Contains(sourceProperty.Name)) continue;

				method.Statements.Add(CreateToTargetAssignmentMethod(
					attr, toTarget, sourceProperty, fromSourceVar.RefVar(), getTypesTranslateAttributeFn));
			}

			method.Statements.Add(toTarget.Return());
			return method;
		}

		public static CodeStatement CreateToTargetAssignmentMethod(
			TranslateAttribute attr,
			CodeParameterDeclarationExpression toTarget,
			PropertyInfo sourceProperty,
			CodeVariableReferenceExpression fromSourceVar,
			Func<Type, Type, TranslateAttribute> getTypesTranslateAttributeFn)
		{

			//model.Name = this.Name;
			var targetProperty = attr.TargetType.GetProperty(sourceProperty.Name);
			var cantWriteToTarget = targetProperty.GetSetMethod() == null;
			if (cantWriteToTarget)
			{
				return new CodeCommentStatement(string.Format("Skipping property 'model.{0}' because 'model.{1}' is read-only",
					targetProperty.Name, sourceProperty.Name));
			}
			var cantReadFromSource = sourceProperty.GetGetMethod() == null;
			if (cantReadFromSource)
			{
				return new CodeCommentStatement(string.Format("Skipping property 'model.{0}' because 'this.{1}' is write-only",
					targetProperty.Name, sourceProperty.Name));
			}

			var areBothTheSameTypes = targetProperty.PropertyType.IsAssignableFrom(sourceProperty.PropertyType);
			if (areBothTheSameTypes)
			{
				return toTarget.Assign(sourceProperty.Name, fromSourceVar.RefProperty(sourceProperty.Name));
			}

			//model.BillingAddress = this.BillingAddress.ToTarget();
			var sourcePropertyType = sourceProperty.PropertyType;
			var targetPropertyType = targetProperty.PropertyType;
			var sourceAttr = getTypesTranslateAttributeFn(sourcePropertyType, targetPropertyType);
			var isSourceTranslatableAlso = sourceAttr != null;
			var useExtensionMethods = attr is TranslateExtensionAttribute;

			if (isSourceTranslatableAlso)
			{
				var toTargetMethodName = sourceAttr.GetConvertToTargetMethodName();
				var method = fromSourceVar.RefProperty(sourceProperty.Name).Call(toTargetMethodName);
				
				return fromSourceVar.RefProperty(sourceProperty.Name).IfIsNotNull(
					toTarget.Assign(sourceProperty.Name, method)
				);
			}

			var sourceIsGenericList = sourcePropertyType.IsGenericType && sourcePropertyType.GetGenericTypeDefinition() == typeof(List<>);
			var targetIsGenericList = targetPropertyType.IsGenericType && targetPropertyType.GetGenericTypeDefinition() == typeof(List<>);
			var bothAreGenericLists = sourceIsGenericList && targetIsGenericList;

			if (bothAreGenericLists)
			{
				//to.PhoneNumbers = from.PhoneNumbers.ToPhoneNumbers();
				var propertyListItemTypeAttr = getTypesTranslateAttributeFn(sourcePropertyType.GetGenericArguments()[0], targetPropertyType.GetGenericArguments()[0]);
				var sourceIsTranslatable = propertyListItemTypeAttr != null;
				if (sourceIsTranslatable)
				{
					var toTargetsMethodName = propertyListItemTypeAttr.GetConvertToTargetsMethodName();
					var method = useExtensionMethods
					             		? fromSourceVar.RefProperty(sourceProperty.Name).Call(toTargetsMethodName)
					             		: propertyListItemTypeAttr.SourceType.Call(toTargetsMethodName, sourceProperty.Name.ThisProperty());
					
					return toTarget.Assign(sourceProperty.Name, method);
				}
			}

			//model.Type = StringConverterUtils.Parse<Target.PhoneNumberType>(this.Type);
			var sourcePropertyIsStringAndTargetIsConvertible = sourceProperty.PropertyType == typeof(string)
				&& StringConverterUtils.CanCreateFromString(targetProperty.PropertyType);
			if (sourcePropertyIsStringAndTargetIsConvertible)
			{
				//model.CardType = StringConverterUtils.Parse<CardType>(this.CardType);
				var methodResult = typeof(StringConverterUtils).CallGeneric(CONVERTER_PARSE_METHOD,
					new[] { targetProperty.PropertyType.GenericDefinition() },
					fromSourceVar.RefProperty(sourceProperty.Name));

				return toTarget.Assign(sourceProperty.Name.ThisProperty(), methodResult);
			}

			// Converting 'System.Collections.Generic.List`1 PhoneNumbers' to 'System.Collections.Generic.List`1 PhoneNumbers' is unsupported
			return new CodeCommentStatement(string.Format("Converting '{0}.{1} {2}' to '{3}.{4} {5}' is unsupported"
				, sourceProperty.PropertyType.Namespace, sourceProperty.PropertyType.Name, sourceProperty.Name
				, targetProperty.PropertyType.Namespace, targetProperty.PropertyType.Name, targetProperty.Name));
		}

		public static CodeTypeMember ConvertToSourceMethod(TranslateAttribute attr)
		{
			var from = attr.TargetType.Param("from");
			var method = DeclareToSourceMethod(attr, from);

			return ConvertToSourceMethod(attr, method, from, GetTypesTranslateAttributeFn);
		}

		public static CodeTypeMember ConvertToSourceMethod(
			TranslateAttribute attr,
			CodeMemberMethod method, CodeParameterDeclarationExpression from,
			Func<Type, Type, TranslateAttribute> getTypesTranslateAttributeFn)
		{
			method.Statements.Add(from.ReturnNullIfNull());

			// targetType to = new T();
			var to = attr.SourceType.DeclareVar("to");
			method.Statements.Add(to);

			var fromTargetTypePropertyNames = attr.TargetType.GetProperties().ToList().Select(x => x.Name);
			foreach (var toSourceTypeProperty in attr.SourceType.GetProperties())
			{
				if (!fromTargetTypePropertyNames.Contains(toSourceTypeProperty.Name)) continue;
				method.Statements.Add(CreateToSourceAssignmentMethod(attr, to, toSourceTypeProperty, from, getTypesTranslateAttributeFn));
			}

			method.Statements.Add(to.Return());
			return method;
		}

		public static CodeStatement CreateToSourceAssignmentMethod(
			TranslateAttribute attr,
			CodeVariableDeclarationStatement toSource,
			PropertyInfo toSourceProperty, CodeParameterDeclarationExpression fromTargetParam,
			Func<Type, Type, TranslateAttribute> getTypesTranslateAttributeFn)
		{

			var fromTargetProperty = attr.TargetType.GetProperty(toSourceProperty.Name);
			var cantReadFromTarget = fromTargetProperty.GetGetMethod() == null;
			if (cantReadFromTarget)
			{
				return new CodeCommentStatement(string.Format("Skipping property 'to.{0}' because 'model.{1}' is write-only",
					toSourceProperty.Name, fromTargetProperty.Name));
			}
			var cantWriteToSource = toSourceProperty.GetSetMethod() == null;
			if (cantWriteToSource)
			{
				return new CodeCommentStatement(string.Format("Skipping property 'to.{0}' because 'to.{1}' is read-only",
					toSourceProperty.Name, toSourceProperty.Name));
			}

			//to[property.Name] = this[property.Name] e.g:
			//	to.Name = from.Name;
			if (fromTargetProperty.PropertyType.IsAssignableFrom(toSourceProperty.PropertyType))
			{
				return toSource.Assign(
					toSource.RefProperty(toSourceProperty.Name),
					fromTargetParam.Name.RefArgument().RefProperty(toSourceProperty.Name));
			}

			//to[property.Name] = from[property.PropertyType.Name].ToTarget() e.g:
			//to.Address = from.Address.ToTarget();

			var toSourcePropertyType = toSourceProperty.PropertyType;
			var fromTargetPropertyType = fromTargetProperty.PropertyType;
			var targetAttr = getTypesTranslateAttributeFn(toSourcePropertyType, fromTargetPropertyType);
			var useExtensionMethods = attr is TranslateExtensionAttribute;
			var isTargetTranslatableAlso = targetAttr != null;

			if (isTargetTranslatableAlso)
			{
				var toSourceMethodName = targetAttr.GetConvertToSourceMethodName();
				var method = useExtensionMethods
									? fromTargetParam.RefProperty(toSourceProperty.Name).Call(toSourceMethodName)
									: toSourcePropertyType.Call(toSourceMethodName, fromTargetParam.RefProperty(toSourceProperty.Name));
				return toSource.Assign(toSourceProperty.Name, method);
			}

			var fromSourceIsGenericList = toSourcePropertyType.IsGenericType && toSourcePropertyType.GetGenericTypeDefinition() == typeof(List<>);
			var toTargetIsGenericList = fromTargetPropertyType.IsGenericType && fromTargetPropertyType.GetGenericTypeDefinition() == typeof(List<>);
			var bothAreGenericLists = fromSourceIsGenericList && toTargetIsGenericList;

			if (bothAreGenericLists)
			{
				//to.PhoneNumbers = from.PhoneNumbers.ToPhoneNumbers();
				var propertyListItemTypeAttr = getTypesTranslateAttributeFn(toSourcePropertyType.GetGenericArguments()[0], fromTargetPropertyType.GetGenericArguments()[0]);
				var sourceIsTranslatable = propertyListItemTypeAttr != null;
				if (sourceIsTranslatable)
				{
					var toSourcesMethodName = propertyListItemTypeAttr.GetConvertToSourcesMethodName();
					var method = useExtensionMethods
					             		? fromTargetParam.RefProperty(fromTargetProperty.Name).Call(toSourcesMethodName)
					             		: propertyListItemTypeAttr.SourceType.Call(toSourcesMethodName, fromTargetParam.RefProperty(fromTargetProperty.Name));

					return toSource.RefProperty(toSourceProperty.Name).Assign(method);
				}
			}

			//to[property.Name] = this[property.Name].ToString() e.g:
			//	to.Name = from.Name;
			if (toSourceProperty.PropertyType == typeof(string)
				&& StringConverterUtils.CanCreateFromString(fromTargetProperty.PropertyType))
			{
				var fromTargetPropertyRef = fromTargetParam.Name.RefArgument().RefProperty(toSourceProperty.Name);
				return fromTargetPropertyRef.IfIsNotNull(
							toSource.Assign(
								toSource.RefProperty(toSourceProperty.Name),
								fromTargetPropertyRef.Call("ToString")));
			}

			// Converting 'System.Collections.Generic.List`1 PhoneNumbers' to 'System.Collections.Generic.List`1 PhoneNumbers' is unsupported
			return new CodeCommentStatement(string.Format("Converting '{0}.{1} {2}' to '{3}.{4} {5}' is unsupported"
				, toSourceProperty.PropertyType.Namespace, toSourceProperty.PropertyType.Name, toSourceProperty.Name
				, fromTargetProperty.PropertyType.Namespace, fromTargetProperty.PropertyType.Name, fromTargetProperty.Name));
		}

		public static CodeTypeMember ConvertToSourcesMethod(TranslateAttribute attr)
		{
			var from = attr.TargetType.Param("from", typeof(IEnumerable<>));
			var method = DeclareToSourcesMethod(attr, from);

			return ConvertToSourcesMethod(attr, method, from);
		}

		public static CodeTypeMember ConvertToSourcesMethod(TranslateAttribute attr, CodeMemberMethod method, CodeParameterDeclarationExpression from)
		{
			method.Statements.Add(from.ReturnNullIfNull());

			var to = "to".DeclareGenericVar(attr.SourceType, typeof(List<>));
			method.Statements.Add(to);

			CodeVariableDeclarationStatement item;
			var iter = from.ForEach(attr.TargetType, out item);
			method.Statements.Add(iter);
			var useExtensionMethod = attr is TranslateExtensionAttribute;
			
			var itemMethod = useExtensionMethod
			                 		? item.Call(attr.GetConvertToSourceMethodName()) 
									: attr.SourceType.Call(attr.GetConvertToSourceMethodName(), item);
			
			iter.Statements.Add(to.Call(LIST_ADD_METHOD, itemMethod));

			method.Statements.Add(to.Return());

			return method;
		}
	}
}