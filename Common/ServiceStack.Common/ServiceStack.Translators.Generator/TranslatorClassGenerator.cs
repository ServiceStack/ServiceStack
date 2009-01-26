using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ServiceStack.Common.Utils;
using ServiceStack.Logging;

namespace ServiceStack.Translators.Generator
{
	public class TranslatorClassGenerator
	{
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

		//model.PhoneNumbers = this.PhoneNumbers.ConvertAll(delegate(PhoneNumber x) { return x.ToModel(); });
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

				var declaration = DeclareType(modelType);
				codeNamespace.Types.Add(declaration);

				foreach (var type in attr.ForTypes)
				{
					declaration.Members.Add(ToModelMethod(modelType, type));
					declaration.Members.Add(ToModelListMethod(modelType, type));
					declaration.Members.Add(UpdateModelMethod(modelType, type));
					declaration.Members.Add(ParseMethod(modelType, type));
					declaration.Members.Add(ParseEnumerableMethod(modelType, type));
				}
				generator.GenerateCodeFromNamespace(codeNamespace, writer, options);

			}
		}

		#region Overridable Implementations
		public static CodeTypeDeclaration DeclareType(Type modelType)
		{
			return new CodeTypeDeclaration {
				IsClass = true,
				Name = modelType.Name,
				IsPartial = true,
				TypeAttributes = TypeAttributes.Public,
			};
		}

		public static CodeMemberMethod DeclareToListMethod(Type toModelType, string methodName, CodeParameterDeclarationExpression from)
		{
			return methodName.DeclareMethod(toModelType.RefGeneric(typeof(List<>)),
				MemberAttributes.Public | MemberAttributes.Static, from);
		}

		public static CodeMemberMethod DeclareToModelMethod(Type toModelType, string methodName)
		{
			return methodName.DeclareMethod(toModelType, MemberAttributes.Public);
		}

		public static CodeMemberMethod DeclareParseMethod(string methodName, Type toDtoType, CodeParameterDeclarationExpression from)
		{
			return methodName.DeclareMethod(toDtoType, MemberAttributes.Public | MemberAttributes.Static, from);
		}

		public static CodeMemberMethod DeclareParseEnumerableMethod(string methodName, Type modelType, CodeParameterDeclarationExpression from)
		{
			return methodName.DeclareMethod(
					modelType.RefGeneric(typeof(List<>)), MemberAttributes.Public | MemberAttributes.Static, from);
		}
		#endregion

		public static string GetToModelMethodName(Type fromDtoType, Type toModelType)
		{
			return toModelType.Name == fromDtoType.Name ? "ToModel" : "To" + toModelType.Name;
		}

		public static string GetToModelListMethodName(Type fromDtoType, Type toModelType)
		{
			return toModelType.Name == fromDtoType.Name ? "ToModelList" : "To" + toModelType.Name + "List";
		}

		public static string GetUpdateMethodName(Type fromDtoType, Type toModelType)
		{
			return toModelType.Name == fromDtoType.Name ? "UpdateModel" : "Update" + toModelType.Name;
		}

		/*
			public static List<Model.PhoneNumber> ToModelList(List<DtoType.PhoneNumber> dtoCustomers)
			{
				var to = new List<Model.PhoneNumber>();
				foreach (var dtoCustomer in dtoCustomers)
				{
					to.Add(dtoCustomer.ToModel());
				}
				return to;
			}
		*/
		public static CodeMemberMethod ToModelListMethod(Type fromDtoType, Type toModelType)
		{
			var from = fromDtoType.Param("from", typeof(IEnumerable<>));
			return ToModelListMethod(fromDtoType, toModelType, from);
		}

		public static CodeMemberMethod ToModelListMethod(Type fromDtoType, Type toModelType, CodeParameterDeclarationExpression from)
		{
			var methodName = GetToModelListMethodName(fromDtoType, toModelType);
			var method = DeclareToListMethod(toModelType, methodName, from);

			method.Statements.Add(from.ReturnNullIfNull());

			var to = "to".DeclareGenericVar(toModelType, typeof(List<>));
			method.Statements.Add(to);

			CodeVariableDeclarationStatement item;
			var iter = from.ForEach(fromDtoType, out item);
			method.Statements.Add(iter);
			var toModelMethodName = GetToModelMethodName(fromDtoType, toModelType);
			iter.Statements.Add(item.IfIsNotNull(to.Call("Add", item.Call(toModelMethodName))));
			//iter.Statements.Add(to.Call("Add", item.Call(toModelMethodName)));

			method.Statements.Add(to.Return());

			return method;
		}

		public static CodeTypeMember ToModelMethod(Type fromDtoType, Type toModelType)
		{
			var methodName = GetToModelMethodName(fromDtoType, toModelType);
			var updateMethodName = GetUpdateMethodName(fromDtoType, toModelType);
			var method = DeclareToModelMethod(toModelType, methodName);
			method.Statements.Add(updateMethodName.Call(toModelType.New()).Return());
			return method;
		}

		public static CodeTypeMember UpdateModelMethod(Type fromDtoType, Type toModelType)
		{
			var methodName = GetUpdateMethodName(fromDtoType, toModelType);
			var toModel = toModelType.Param("model");
			var method = methodName.DeclareMethod(toModelType, MemberAttributes.Public, toModel);

			var typeNames = toModelType.GetProperties().ToList().Select(x => x.Name);
			var fromDtoVar = fromDtoType.DeclareVar("from", new CodeThisReferenceExpression());
			method.Statements.Add(fromDtoVar);
			foreach (var fromDtoProperty in fromDtoType.GetProperties())
			{
				if (!typeNames.Contains(fromDtoProperty.Name)) continue;

				method.Statements.Add(CreateToModelAssignmentMethod(toModel, fromDtoProperty, toModelType, fromDtoVar.RefVar()));
			}

			method.Statements.Add(toModel.Return());
			return method;
		}

		public static CodeStatement CreateToModelAssignmentMethod(CodeParameterDeclarationExpression toModel,
			PropertyInfo fromDtoProperty, Type toModelType, CodeVariableReferenceExpression fromDtoVar)
		{

			//model.Name = this.Name;
			var toModelProperty = toModelType.GetProperty(fromDtoProperty.Name);
			var modelCantWrite = toModelProperty.GetSetMethod() == null;
			if (modelCantWrite)
			{
				return new CodeCommentStatement(string.Format("Skipping property 'model.{0}' because 'model.{1}' is read-only",
					toModelProperty.Name, fromDtoProperty.Name));
			}
			var dtoCantRead = fromDtoProperty.GetGetMethod() == null;
			if (dtoCantRead)
			{
				return new CodeCommentStatement(string.Format("Skipping property 'model.{0}' because 'this.{1}' is write-only",
					toModelProperty.Name, fromDtoProperty.Name));
			}

			var areBothTheSameTypes = toModelProperty.PropertyType.IsAssignableFrom(fromDtoProperty.PropertyType);
			if (areBothTheSameTypes)
			{
				return toModel.Assign(fromDtoProperty.Name, fromDtoVar.RefProperty(fromDtoProperty.Name));
			}

			//model.BillingAddress = this.BillingAddress.ToModel();
			var fromDtoPropertyType = fromDtoProperty.PropertyType;
			var toModelPropertyType = toModelProperty.PropertyType;
			var isModelAlso = fromDtoProperty.PropertyType.GetCustomAttributes(typeof(TranslateModelAttribute), false).Count() > 0;
			if (isModelAlso)
			{
				var toModelMethodName = GetToModelMethodName(fromDtoPropertyType, toModelPropertyType);
				return fromDtoVar.RefProperty(fromDtoProperty.Name).IfIsNotNull(
					toModel.Assign(fromDtoProperty.Name, fromDtoVar.RefProperty(fromDtoProperty.Name).Call(toModelMethodName))
				);
			}

			var fromDtoIsGenericList = fromDtoPropertyType.IsGenericType && fromDtoPropertyType.GetGenericTypeDefinition() == typeof(List<>);
			var toModelIsGenericList = toModelPropertyType.IsGenericType && toModelPropertyType.GetGenericTypeDefinition() == typeof(List<>);
			var bothAreGenericLists = fromDtoIsGenericList && toModelIsGenericList;

			if (bothAreGenericLists)
			{
				//PhoneNumber.ToModelList(this.PhoneNumbers);
				var fromDtoIsModel = fromDtoPropertyType.GetGenericArguments()[0].GetCustomAttributes(typeof(TranslateModelAttribute), false).Count() > 0;
				if (fromDtoIsModel)
				{
					var toModelListMethodName = GetToModelListMethodName(fromDtoPropertyType, toModelPropertyType);
					return toModel.Assign(fromDtoProperty.Name, fromDtoPropertyType.GetGenericArguments()[0].Call(toModelListMethodName, fromDtoVar.RefProperty(fromDtoProperty.Name)));
				}
			}

			//model.Type = StringConverterUtils.Parse<Model.PhoneNumberType>(this.Type);
			var dtoPropertyIsStringAndModelIsConvertible = fromDtoProperty.PropertyType == typeof(string)
				&& StringConverterUtils.CanCreateFromString(toModelProperty.PropertyType);
			if (dtoPropertyIsStringAndModelIsConvertible)
			{
				//model.CardType = StringConverterUtils.Parse<CardType>(this.CardType);
				var methodResult = typeof(StringConverterUtils).CallGeneric("Parse",
					new[] { toModelProperty.PropertyType.GenericDefinition() },
					fromDtoVar.RefProperty(fromDtoProperty.Name));

				return toModel.Assign(fromDtoProperty.Name.ThisProperty(), methodResult);
			}

			// Converting 'System.Collections.Generic.List`1 PhoneNumbers' to 'System.Collections.Generic.List`1 PhoneNumbers' is unsupported
			return new CodeCommentStatement(string.Format("Converting '{0}.{1} {2}' to '{3}.{4} {5}' is unsupported"
				, fromDtoProperty.PropertyType.Namespace, fromDtoProperty.PropertyType.Name, fromDtoProperty.Name
				, toModelProperty.PropertyType.Namespace, toModelProperty.PropertyType.Name, toModelProperty.Name));
		}

		public static CodeTypeMember ParseMethod(Type toDtoType, Type fromModelType)
		{
			var methodName = "Parse";
			var from = fromModelType.Param("from");
			var method = DeclareParseMethod(methodName, toDtoType, from);

			return ParseMethod(toDtoType, fromModelType, method, from);
		}

		public static CodeTypeMember ParseMethod(Type toDtoType, Type fromModelType, CodeMemberMethod method, CodeParameterDeclarationExpression from)
		{
			method.Statements.Add(from.ReturnNullIfNull());

			// modelType to = new T();
			var to = toDtoType.DeclareVar("to");
			method.Statements.Add(to);

			var fromModelTypePropertyNames = fromModelType.GetProperties().ToList().Select(x => x.Name);
			foreach (var toDtoTypeProperty in toDtoType.GetProperties())
			{
				if (!fromModelTypePropertyNames.Contains(toDtoTypeProperty.Name)) continue;
				method.Statements.Add(CreateToDtoAssignmentMethod(to, toDtoTypeProperty, fromModelType, from));
			}

			method.Statements.Add(to.Return());
			return method;
		}

		public static CodeStatement CreateToDtoAssignmentMethod(CodeVariableDeclarationStatement toDto,
			PropertyInfo toDtoTypeProperty, Type fromModelType, CodeParameterDeclarationExpression fromModelParam)
		{

			var fromModelProperty = fromModelType.GetProperty(toDtoTypeProperty.Name);
			var modelCantRead = fromModelProperty.GetGetMethod() == null;
			if (modelCantRead)
			{
				return new CodeCommentStatement(string.Format("Skipping property 'to.{0}' because 'model.{1}' is write-only",
					toDtoTypeProperty.Name, fromModelProperty.Name));
			}
			var dtoCantWrite = toDtoTypeProperty.GetSetMethod() == null;
			if (dtoCantWrite)
			{
				return new CodeCommentStatement(string.Format("Skipping property 'to.{0}' because 'to.{1}' is read-only",
					toDtoTypeProperty.Name, toDtoTypeProperty.Name));
			}

			//to[property.Name] = this[property.Name] e.g:
			//	to.Name = from.Name;
			if (fromModelProperty.PropertyType.IsAssignableFrom(toDtoTypeProperty.PropertyType))
			{
				return toDto.Assign(
					toDto.RefProperty(toDtoTypeProperty.Name),
					fromModelParam.Name.RefArgument().RefProperty(toDtoTypeProperty.Name));
			}

			//to[property.Name] = from[property.PropertyType.Name].ToModel() e.g:
			//to.Address = from.Address.ToModel();
			var isModelAlso = toDtoTypeProperty.PropertyType.GetCustomAttributes(typeof(TranslateModelAttribute), false).Count() > 0;
			if (isModelAlso)
			{
				return toDto.Assign(toDtoTypeProperty.Name, toDtoTypeProperty.PropertyType.Call("Parse", fromModelParam.RefProperty(toDtoTypeProperty.Name)));
			}

			var toDtoTypePropertyType = toDtoTypeProperty.PropertyType;
			var fromModelPropertyType = fromModelProperty.PropertyType;
			var fromDtoIsGenericList = toDtoTypePropertyType.IsGenericType && toDtoTypePropertyType.GetGenericTypeDefinition() == typeof(List<>);
			var toModelIsGenericList = fromModelPropertyType.IsGenericType && fromModelPropertyType.GetGenericTypeDefinition() == typeof(List<>);
			var bothAreGenericLists = fromDtoIsGenericList && toModelIsGenericList;

			if (bothAreGenericLists)
			{
				//to.PhoneNumbers = PhoneNumber.ParseAll(this.PhoneNumbers);
				var fromDtoIsModel = toDtoTypePropertyType.GetGenericArguments()[0].GetCustomAttributes(typeof(TranslateModelAttribute), false).Count() > 0;
				if (fromDtoIsModel)
				{
					return toDto.RefProperty(toDtoTypeProperty.Name).Assign(toDtoTypePropertyType.GetGenericArguments()[0].Call("ParseAll", fromModelParam.RefProperty(fromModelProperty.Name)));
					//return toDto.Assign(fromModelPropertyType.Name, toDtoTypePropertyType.GetGenericArguments()[0].Call("ParseAll", fromModelProperty.Name.ThisProperty()));
				}
			}

			//to[property.Name] = this[property.Name].ToString() e.g:
			//	to.Name = from.Name;
			if (toDtoTypeProperty.PropertyType == typeof(string)
				&& StringConverterUtils.CanCreateFromString(fromModelProperty.PropertyType))
			{
				var fromModelPropertyRef = fromModelParam.Name.RefArgument().RefProperty(toDtoTypeProperty.Name);
				return fromModelPropertyRef.IfIsNotNull(
							toDto.Assign(
								toDto.RefProperty(toDtoTypeProperty.Name),
								fromModelPropertyRef.Call("ToString")));
			}

			// Converting 'System.Collections.Generic.List`1 PhoneNumbers' to 'System.Collections.Generic.List`1 PhoneNumbers' is unsupported
			return new CodeCommentStatement(string.Format("Converting '{0}.{1} {2}' to '{3}.{4} {5}' is unsupported"
				, toDtoTypeProperty.PropertyType.Namespace, toDtoTypeProperty.PropertyType.Name, toDtoTypeProperty.Name
				, fromModelProperty.PropertyType.Namespace, fromModelProperty.PropertyType.Name, fromModelProperty.Name));
		}

		public static CodeTypeMember ParseEnumerableMethod(Type dtoType, Type modelType)
		{
			var methodName = "ParseAll";
			var from = modelType.Param("from", typeof(IEnumerable<>));
			var method = DeclareParseEnumerableMethod(methodName, dtoType, from);

			return ParseEnumerableMethod(dtoType, modelType, method, from);
		}

		public static CodeTypeMember ParseEnumerableMethod(Type dtoType, Type modelType, CodeMemberMethod method, CodeParameterDeclarationExpression from)
		{
			method.Statements.Add(from.ReturnNullIfNull());

			var to = "to".DeclareGenericVar(dtoType, typeof(List<>));
			method.Statements.Add(to);

			CodeVariableDeclarationStatement item;
			var iter = from.ForEach(modelType, out item);
			method.Statements.Add(iter);
			iter.Statements.Add(to.Call("Add", dtoType.Call("Parse", item)));

			method.Statements.Add(to.Return());

			return method;
		}
	}
}