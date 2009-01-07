using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using Boo.Lang.CodeDom;
using Microsoft.CSharp;
using Microsoft.FSharp.Compiler.CodeDom;
using Microsoft.JScript;
using Microsoft.VisualBasic;

namespace ServiceStack.Translators.Generator
{
	public enum CodeLang
	{
		CSharp,
		JScript,
		Vb,
		Python,
		Boo,
		FSharp,
		//Java //JSharp, 
	}

	public static class CodeDomUtils
	{
		public static ICodeGenerator CreateGenerator(CodeLang langType)
		{
			switch (langType)
			{
				case CodeLang.CSharp:
					return new CSharpCodeProvider().CreateGenerator();
				//case CodeLang.FSharp:
				//    return new FSharpCodeProvider().CreateGenerator();
				case CodeLang.JScript:
					return new JScriptCodeProvider().CreateGenerator();
				case CodeLang.Vb:
					return new VBCodeProvider().CreateGenerator();
				//case CodeLang.Boo:
				//    return new BooCodeProvider().CreateGenerator();
				default:
					throw new NotSupportedException();
			}
		}

		/// <summary>
		/// type.New() == new T()
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns></returns>
		public static CodeObjectCreateExpression New(this Type type)
		{
			return new CodeObjectCreateExpression(new CodeTypeReference(type));
		}

		public static CodeObjectCreateExpression New(this string type)
		{
			return new CodeObjectCreateExpression(new CodeTypeReference(type));
		}

		/// <summary>
		/// Declares the method.
		/// 
		/// methodName.DeclareMethod(typeof(string), MemberAttributes.Public):
		///		public virtual string methodName()
		/// 
		/// methodName.DeclareMethod(typeof(string), MemberAttributes.Public, typeof(int).Param("argument")):
		///		public virtual string methodName(int argument)
		/// 
		/// </summary>
		/// <param name="methodName">Name of the method.</param>
		/// <param name="returnType">Type of the return.</param>
		/// <param name="attributes">The attributes.</param>
		/// <param name="methodParams">The method params.</param>
		/// <returns></returns>
		public static CodeMemberMethod DeclareMethod(this string methodName, Type returnType, MemberAttributes attributes,
			params CodeParameterDeclarationExpression[] methodParams)
		{
			return DeclareMethod(methodName, new CodeTypeReference(returnType.FullName), attributes, methodParams);
		}

		public static CodeMemberMethod DeclareMethod(this string methodName, CodeTypeReference returnType, MemberAttributes attributes,
			params CodeParameterDeclarationExpression[] methodParams)
		{
			var method = new CodeMemberMethod {
				Name = methodName,
				ReturnType = returnType,
				Attributes = attributes,
			};
			foreach (var methodParam in methodParams)
			{
				method.Parameters.Add(methodParam);
			}
			return method;
		}

		public static CodeVariableDeclarationStatement DeclareVar(this Type type, string variableName)
		{
			return new CodeVariableDeclarationStatement(type, variableName, New(type));
		}

		public static CodeVariableDeclarationStatement DeclareVar(this Type type, string variableName, CodeExpression initExpression)
		{
			return new CodeVariableDeclarationStatement(type, variableName, initExpression);
		}

		public static CodeMethodReturnStatement Return(this CodeVariableDeclarationStatement expression)
		{
			return new CodeMethodReturnStatement(new CodeArgumentReferenceExpression(expression.Name));
		}

		public static CodeMethodReturnStatement Return(this CodeParameterDeclarationExpression expression)
		{
			return new CodeMethodReturnStatement(new CodeArgumentReferenceExpression(expression.Name));
		}

		public static CodeMethodReturnStatement Return(this CodeMethodInvokeExpression expression)
		{
			return new CodeMethodReturnStatement(expression);
		}

		public static CodeParameterDeclarationExpression Param(this Type type, string paramName)
		{
			return new CodeParameterDeclarationExpression(type, paramName);
		}

		/// <summary>
		/// type.Param("paramName", typeof(List&gt;T&lt;)):
		///		[Method(..] List&gt;T&lt; paramName
		/// </summary>
		/// <param name="type">The type.</param>
		/// <param name="paramName">Name of the param.</param>
		/// <param name="genericTypeDefinitionName">Name of the generic type definition.</param>
		/// <returns></returns>
		public static CodeParameterDeclarationExpression Param(this Type type, string paramName, Type genericTypeDefinitionName)
		{
			return new CodeParameterDeclarationExpression(
				new CodeTypeReference(genericTypeDefinitionName.FullName, new CodeTypeReference(type)), paramName);
		}


		public static CodeAssignStatement Assign(this CodeParameterDeclarationExpression property, string assignTo, string assignFrom)
		{
			return new CodeAssignStatement(
				new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(property.Name), assignTo),
				new CodeArgumentReferenceExpression(assignFrom));
		}

		public static CodeAssignStatement Assign(this CodeParameterDeclarationExpression property, string assignTo, CodePropertyReferenceExpression assignFrom)
		{
			return new CodeAssignStatement(
				new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(property.Name), assignTo), assignFrom);
		}

		//UpdateModelMethod: model.Assign(property.Name, property.Name.ThisProperty().Call("ToModel"))
		public static CodeAssignStatement Assign(this CodeParameterDeclarationExpression property, string assignTo, CodeMethodInvokeExpression methodResult)
		{
			return new CodeAssignStatement(
				new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(property.Name), assignTo),
				methodResult);
		}

		public static CodeAssignStatement Assign(this CodeVariableDeclarationStatement property, string assignTo, string assignFrom)
		{
			return new CodeAssignStatement(
				new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(property.Name), assignTo),
				new CodeArgumentReferenceExpression(assignFrom));
		}

		//to.Assign(property.Name, from.RefProperty(property.Name).Call("ToModel"))
		public static CodeAssignStatement Assign(this CodeVariableDeclarationStatement property, string assignTo, CodeExpression assignExpression)
		{
			return new CodeAssignStatement(
				new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(property.Name), assignTo),
				assignExpression);
		}

		public static CodeAssignStatement Assign(this CodeVariableDeclarationStatement property, CodeExpression assignTo, CodeExpression expression)
		{
			return new CodeAssignStatement(assignTo, expression);
		}

		public static CodePropertyReferenceExpression ThisProperty(this string propertyName)
		{
			return new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), propertyName);
		}

		public static CodePropertyReferenceExpression RefProperty(this string propertyName, CodeExpression field)
		{
			return new CodePropertyReferenceExpression(field, propertyName);
		}

		public static CodePropertyReferenceExpression RefProperty(this CodeExpression field, string propertyName)
		{
			return new CodePropertyReferenceExpression(field, propertyName);
		}

		//public static CodeMethodReferenceExpression RefMethod(this string methodName)
		//{
		//    return new CodeMethodReferenceExpression(new CodeTypeReferenceExpression(type), methodName);
		//}

		/// <summary>
		/// Refs the argument.
		/// 
		/// "arg".RefArgument() == arg.
		/// </summary>
		/// <param name="argumentName">Name of the argument.</param>
		/// <returns></returns>
		public static CodeArgumentReferenceExpression RefArgument(this string argumentName)
		{
			return new CodeArgumentReferenceExpression(argumentName);
		}

		/// <summary>
		/// Refs the property.
		/// 
		/// typeof(int).Param("from").RefProperty("propertyName"):
		///		[void Method(...] int from
		///			from.propertyName
		/// </summary>
		/// <param name="param">The param.</param>
		/// <param name="propertyName">Name of the property.</param>
		/// <returns></returns>
		public static CodePropertyReferenceExpression RefProperty(this CodeParameterDeclarationExpression param, string propertyName)
		{
			return new CodePropertyReferenceExpression(new CodeArgumentReferenceExpression(param.Name), propertyName);
		}

		public static CodePropertyReferenceExpression RefProperty(this CodePropertyReferenceExpression param, string propertyName)
		{
			return new CodePropertyReferenceExpression(param, propertyName);
		}

		/// <summary>
		/// Calls the specified member.
		/// 
		/// "propertyName".ThisProperty().Call("ToModel") == this.propertyName.ToModel()
		/// 
		/// </summary>
		/// <param name="member">The member.</param>
		/// <param name="methodName">Name of the method.</param>
		/// <param name="methodParams">The method params.</param>
		/// <returns></returns>
		public static CodeMethodInvokeExpression Call(this CodePropertyReferenceExpression member, string methodName, params CodeExpression[] methodParams)
		{
			return new CodeMethodInvokeExpression(member, methodName, methodParams);
		}

		public static CodeMethodInvokeExpression Call(this string methodName, params CodeExpression[] methodParams)
		{
			return new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), methodName, methodParams);
		}

		public static CodeMethodInvokeExpression Call(this CodeVariableDeclarationStatement var, string methodName, params CodeExpression[] methodParams)
		{
			return new CodeMethodInvokeExpression(var.Name.RefArgument(), methodName, methodParams);
		}

		public static CodeMethodInvokeExpression Call(this CodeParameterDeclarationExpression var, string methodName, params CodeExpression[] methodParams)
		{
			return new CodeMethodInvokeExpression(var.Name.RefArgument(), methodName, methodParams);
		}

		/// <summary>
		/// Calls the specified method name.
		/// 
		/// var variable = type.DeclareVar("argument");
		/// "methodName".Call(variable) == this.UpdateModel(argument);
		/// </summary>
		/// <param name="methodName">Name of the method.</param>
		/// <param name="varMethodParams">The var method params.</param>
		/// <returns></returns>
		public static CodeMethodInvokeExpression Call(this string methodName, params CodeVariableDeclarationStatement[] varMethodParams)
		{
			var methodParams = varMethodParams.ToList().ConvertAll(x => x.Name.RefArgument()).ToArray();
			return new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), methodName, methodParams);
		}

		public static CodeMethodInvokeExpression Call(this Type type, string methodName, params CodeExpression[] methodParams)
		{
			return new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(type), methodName, methodParams);
		}

		public static CodeMethodInvokeExpression Call(this Type type, string methodName, params CodeVariableDeclarationStatement[] varMethodParams)
		{
			var methodParams = varMethodParams.ToList().ConvertAll(x => x.Name.RefArgument()).ToArray();
			return new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(type), methodName, methodParams);
		}

		public static CodeTypeReference RefGeneric(this Type type, Type genericTypeDefinition)
		{
			return new CodeTypeReference(genericTypeDefinition.FullName, new CodeTypeReference(type));
		}

		//"to".DeclareGenericVar(type, typeof(List<>)):
		//
		//List<type> to = new ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer();
		public static CodeVariableDeclarationStatement DeclareGenericVar(this string variableName, Type type, Type genericTypeDefinition)
		{
			return new CodeVariableDeclarationStatement(
				type.RefGeneric(genericTypeDefinition), variableName, type.NewGeneric(genericTypeDefinition));
		}

		/// <summary>
		/// Declares the generic var.
		/// 
		/// IEnumerable<itemType> iter = enumerableVar.GetEnumerator()
		/// NOTE: Always leave the genericTypeDefinition at the end so we can use params[] to support more complex generic types.
		/// </summary>
		/// <param name="variableName">Name of the variable.</param>
		/// <param name="type">The type.</param>
		/// <param name="initExpression">The init expression.</param>
		/// <param name="genericTypeDefinition">The generic type definition.</param>
		/// <returns></returns>
		public static CodeVariableDeclarationStatement DeclareGenericVar(this string variableName, Type type, CodeExpression initExpression, Type genericTypeDefinition)
		{
			return new CodeVariableDeclarationStatement(
				type.RefGeneric(genericTypeDefinition), variableName, initExpression);
		}

		public static CodeObjectCreateExpression NewGeneric(this Type type, Type genericTypeDefinition)
		{
			return new CodeObjectCreateExpression(type.RefGeneric(genericTypeDefinition));
		}

		public static CodePropertyReferenceExpression RefProperty(this CodeVariableDeclarationStatement varDeclaration, string propertyName)
		{
			return new CodePropertyReferenceExpression(new CodeArgumentReferenceExpression(varDeclaration.Name), propertyName);
		}

		public static CodeIterationStatement ForEach(this CodeParameterDeclarationExpression enumerableVar, Type itemType, out CodeVariableDeclarationStatement item)
		{
			//IEnumerator<itemType> iter = enumerableVar.GetEnumerator()
			var iter = "iter".DeclareGenericVar(itemType, enumerableVar.Call("GetEnumerator"), typeof(IEnumerator<>));

			var iterStatement = new CodeIterationStatement {
				InitStatement = iter,
				TestExpression = iter.Call("MoveNext"),
				IncrementStatement = new CodeSnippetStatement(""),
			};

			item = itemType.DeclareVar("item", iter.RefProperty("Current"));
			iterStatement.Statements.Add(item);
			return iterStatement;
		}

		/// <summary>
		/// Calls the specified method name.
		/// 
		/// "methodName".Call(type.New()) == this.UpdateModel(new T());
		/// </summary>
		/// <param name="methodName">Name of the method.</param>
		/// <param name="methodParams">The method params.</param>
		/// <returns></returns>
		public static CodeMethodInvokeExpression Call(this string methodName, params CodeObjectCreateExpression[] methodParams)
		{
			return new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), methodName, methodParams);
		}
	}
}