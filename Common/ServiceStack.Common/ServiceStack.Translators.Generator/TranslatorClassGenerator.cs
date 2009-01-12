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
					declaration.Members.Add(ToModelListMethod(modelType, type));
					declaration.Members.Add(UpdateModelMethod(modelType, type));
					declaration.Members.Add(ParseMethod(modelType, type));
					declaration.Members.Add(ParseEnumerableMethod(modelType, type));
				}
				generator.GenerateCodeFromNamespace(codeNamespace, writer, options);

			}
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
		private static CodeMemberMethod ToModelListMethod(Type fromDtoType, Type toModelType)
		{
			var methodName = toModelType.Name == fromDtoType.Name ? "ToModelList" : "To" + toModelType.Name + "List";
			var from = fromDtoType.Param("from", typeof(IEnumerable<>));
			var method = methodName.DeclareMethod(toModelType.RefGeneric(typeof(List<>)), MemberAttributes.Public | MemberAttributes.Static, from);

			var to = "to".DeclareGenericVar(toModelType, typeof(List<>));
			method.Statements.Add(to);

			CodeVariableDeclarationStatement item;
			var iter = from.ForEach(fromDtoType, out item);
			method.Statements.Add(iter);
			iter.Statements.Add(to.Call("Add", item.Call("ToModel")));

			method.Statements.Add(to.Return());

			return method;
		}

		private static CodeMemberMethod ToModelMethod(Type fromDtoType, Type toModelType)
		{
			var methodName = toModelType.Name == fromDtoType.Name ? "ToModel" : "To" + toModelType.Name;
			var method = methodName.DeclareMethod(toModelType, MemberAttributes.Public);
			method.Statements.Add("UpdateModel".Call(toModelType.New()).Return());
			return method;
		}

		private static CodeMemberMethod UpdateModelMethod(Type fromDtoType, Type toModelType)
		{
			var methodName = toModelType.Name == fromDtoType.Name ? "UpdateModel" : "Update" + toModelType.Name;
			var toModel = toModelType.Param("model");
			var method = methodName.DeclareMethod(toModelType, MemberAttributes.Public, toModel);

			var typeNames = toModelType.GetProperties().ToList().Select(x => x.Name);
			foreach (var fromDtoProperty in fromDtoType.GetProperties())
			{
				if (!typeNames.Contains(fromDtoProperty.Name)) continue;

				method.Statements.Add(CreateToModelAssignmentMethod(toModel, fromDtoProperty, toModelType));
			}

			method.Statements.Add(toModel.Return());
			return method;
		}

		private static CodeStatement CreateToModelAssignmentMethod(CodeParameterDeclarationExpression toModel, 
			PropertyInfo fromDtoProperty, Type toModelType)
		{

			//model.Name = Name;
			var toModelProperty = toModelType.GetProperty(fromDtoProperty.Name);
			var areBothTheSameTypes = toModelProperty.PropertyType.IsAssignableFrom(fromDtoProperty.PropertyType);
			if (areBothTheSameTypes)
			{
				return toModel.Assign(fromDtoProperty.Name, fromDtoProperty.Name);
			}

			//model.BillingAddress = this.BillingAddress.ToModel();
			var isModelAlso = fromDtoProperty.PropertyType.GetCustomAttributes(typeof(TranslateModelAttribute), false).Count() > 0;
			if (isModelAlso)
			{
				return toModel.Assign(fromDtoProperty.Name, fromDtoProperty.Name.ThisProperty().Call("ToModel"));
			}

			var fromDtoPropertyType = fromDtoProperty.PropertyType;
			var toModelPropertyType = toModelProperty.PropertyType;
			var fromDtoIsGenericList = fromDtoPropertyType.IsGenericType && fromDtoPropertyType.GetGenericTypeDefinition() == typeof(List<>);
			var toModelIsGenericList = toModelPropertyType.IsGenericType && toModelPropertyType.GetGenericTypeDefinition() == typeof(List<>);
			var bothAreGenericLists = fromDtoIsGenericList && toModelIsGenericList;

			if (bothAreGenericLists)
			{
				//PhoneNumber.ToModelList(this.PhoneNumbers);
				var fromDtoIsModel = fromDtoPropertyType.GetGenericArguments()[0].GetCustomAttributes(typeof(TranslateModelAttribute), false).Count() > 0;
				if (fromDtoIsModel)
				{
					return toModel.Assign(fromDtoProperty.Name, fromDtoPropertyType.GetGenericArguments()[0].Call("ToModelList", fromDtoProperty.Name.ThisProperty()));
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
					fromDtoProperty.Name.ThisProperty());

				return toModel.Assign(fromDtoProperty.Name.ThisProperty(), methodResult);
			}

			// Converting 'System.Collections.Generic.List`1 PhoneNumbers' to 'System.Collections.Generic.List`1 PhoneNumbers' is unsupported
			return new CodeCommentStatement(string.Format("Converting '{0}.{1} {2}' to '{3}.{4} {5}' is unsupported"
				, fromDtoProperty.PropertyType.Namespace, fromDtoProperty.PropertyType.Name, fromDtoProperty.Name
				, toModelProperty.PropertyType.Namespace, toModelProperty.PropertyType.Name, toModelProperty.Name));
		}

		private static CodeMemberMethod ParseMethod(Type toDtoType, Type fromModelType)
		{
			var methodName = "Parse";
			var from = fromModelType.Param("from");
			var method = methodName.DeclareMethod(toDtoType, MemberAttributes.Public | MemberAttributes.Static, from);

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

		private static CodeStatement CreateToDtoAssignmentMethod(CodeVariableDeclarationStatement toDto,
			PropertyInfo toDtoTypeProperty, Type fromModelType, CodeParameterDeclarationExpression fromModelParam)
		{

			//to[property.Name] = this[property.Name] e.g:
			//	to.Name = from.Name;
			var fromModelProperty = fromModelType.GetProperty(toDtoTypeProperty.Name);
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
				return toDto.Assign(
					toDto.RefProperty(toDtoTypeProperty.Name),
					fromModelParam.Name.RefArgument().RefProperty(toDtoTypeProperty.Name).Call("ToString"));
			}

			// Converting 'System.Collections.Generic.List`1 PhoneNumbers' to 'System.Collections.Generic.List`1 PhoneNumbers' is unsupported
			return new CodeCommentStatement(string.Format("Converting '{0}.{1} {2}' to '{3}.{4} {5}' is unsupported"
				, toDtoTypeProperty.PropertyType.Namespace, toDtoTypeProperty.PropertyType.Name, toDtoTypeProperty.Name
				, fromModelProperty.PropertyType.Namespace, fromModelProperty.PropertyType.Name, fromModelProperty.Name));
		}

		private static CodeMemberMethod ParseEnumerableMethod(Type modelType, Type type)
		{
			var methodName = "ParseAll";
			//var from = new CodeParameterDeclarationExpression(new CodeTypeReference("IEnumerable", new CodeTypeReference(type)), "from");
			var from = type.Param("from", typeof(IEnumerable<>));

			// public static List<modelType> methodName(IEnumerable<modelType> from) e.g:
			// public static List<T> ParseAll(IEnumerable<T> from)
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