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

		public TranslatorClassGenerator()
			: this(CodeLang.CSharp)
		{
		}

		public TranslatorClassGenerator(CodeLang lang)
		{
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

		private static CodeMemberMethod UpdateModelMethod(Type fromDtoType, Type toModelType)
		{
			var methodName = toModelType.Name == fromDtoType.Name ? "UpdateModel" : "Update" + toModelType.Name;
			var toModel = toModelType.Param("model");
			var method = methodName.DeclareMethod(toModelType, MemberAttributes.Public, toModel);

			var typeNames = toModelType.GetProperties().ToList().Select(x => x.Name);
			foreach (var fromDtoProperty in fromDtoType.GetProperties())
			{
				if (!typeNames.Contains(fromDtoProperty.Name)) continue;

				var isModelAlso = fromDtoProperty.PropertyType.GetCustomAttributes(typeof(TranslateModelAttribute), false).Count() > 0;
				if (isModelAlso)
				{
					method.Statements.Add(toModel.Assign(fromDtoProperty.Name, fromDtoProperty.Name.ThisProperty().Call("ToModel")));
				}
				else
				{
					var toModelProperty = toModelType.GetProperty(fromDtoProperty.Name);
					if (toModelProperty.PropertyType.IsAssignableFrom(fromDtoProperty.PropertyType))
					{
						method.Statements.Add(toModel.Assign(fromDtoProperty.Name, fromDtoProperty.Name));
					}
					else
					{
						if (fromDtoProperty.PropertyType == typeof(string)
							&& StringConverterUtils.CanCreateFromString(toModelProperty.PropertyType))
						{
							//model.CardType = StringConverterUtils.Parse<CardType>(this.CardType);
							var methodResult = typeof(StringConverterUtils).CallGeneric("Parse",
								new[] { toModelProperty.PropertyType.GenericDefinition() },
								fromDtoProperty.Name.ThisProperty());

							method.Statements.Add(toModel.Assign(fromDtoProperty.Name.ThisProperty(), methodResult));
						}
					}
				}
			}

			method.Statements.Add(toModel.Return());
			return method;
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

				var isModelAlso = toDtoTypeProperty.PropertyType.GetCustomAttributes(typeof(TranslateModelAttribute), false).Count() > 0;
				if (isModelAlso)
				{
					//to[property.Name] = from[property.PropertyType.Name].ToModel() e.g:
					//	to.Address = from.Address.ToModel();
					method.Statements.Add(to.Assign(toDtoTypeProperty.Name, toDtoTypeProperty.PropertyType.Call("Parse", from.RefProperty(toDtoTypeProperty.Name))));
				}
				else
				{
					//to.CardType = from.CardType[.ToString()];
					var fromModelTypeProperty = fromModelType.GetProperty(toDtoTypeProperty.Name);
					if (fromModelTypeProperty.PropertyType.IsAssignableFrom(toDtoTypeProperty.PropertyType))
					{
						//to[property.Name] = this[property.Name] e.g:
						//	to.Name = from.Name;
						method.Statements.Add(to.Assign(
							to.RefProperty(toDtoTypeProperty.Name),
							from.Name.RefArgument().RefProperty(toDtoTypeProperty.Name)));
					}
					else
					{
						if (toDtoTypeProperty.PropertyType == typeof(string)
							&& StringConverterUtils.CanCreateFromString(fromModelTypeProperty.PropertyType))
						{
							//to[property.Name] = this[property.Name].ToString() e.g:
							//	to.Name = from.Name;
							method.Statements.Add(to.Assign(
								to.RefProperty(toDtoTypeProperty.Name),
								from.Name.RefArgument().RefProperty(toDtoTypeProperty.Name).Call("ToString")));
						}
					}
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