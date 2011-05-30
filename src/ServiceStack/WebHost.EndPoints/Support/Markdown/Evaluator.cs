using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml;
using System.Text;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.WebHost.EndPoints.Support.Markdown
{
	public class EvaluatorExecutionContext
	{
		public EvaluatorExecutionContext()
		{
			this.Items = new List<EvaluatorItem>();
			this.TypeProperties = new Dictionary<string, Type>();
		}

		public Type BaseType { get; set; }
		public Type[] GenericArgs { get; set; }
		public IDictionary<string, Type> TypeProperties { get; set; }

		public List<EvaluatorItem> Items { get; private set; }

		public Evaluator Build()
		{
			return Items.Count == 0
				? null
				: new Evaluator(Items, BaseType, GenericArgs, TypeProperties);
		}
	}

	public class Evaluator
	{
		const string StaticMethodName = "__tmp";
		Assembly compiledAssembly;
		Type compiledType = null;
		object compiled = null;
		EmptyCtorDelegate compiledTypeCtorFn;

		private Type BaseType { get; set; }
		private Type[] GenericArgs { get; set; }
		private IDictionary<string, Type> TypeProperties { get; set; }

		public Evaluator(IEnumerable<EvaluatorItem> items)
			: this(items, null, null, null)
		{
		}

		public Evaluator(IEnumerable<EvaluatorItem> items,
			Type baseType, Type[] genericArgs, IDictionary<string, Type> typeProperties)
		{
			this.BaseType = baseType;
			this.GenericArgs = genericArgs ?? new Type[0];
			this.TypeProperties = typeProperties;

			ConstructEvaluator(items);
		}

		public Evaluator(Type returnType, string expression, string name)
			: this(returnType, expression, name, null) { }

		public Evaluator(Type returnType, string expression, string name, IDictionary<string, Type> exprParams)
		{
			EvaluatorItem[] items = 
			{
				new EvaluatorItem {
	                ReturnType  = returnType, 
					Expression = expression, 
					Name = name,
					Params = exprParams ?? new Dictionary<string, Type>(),
				}
			};
			ConstructEvaluator(items);
		}

		public Evaluator(EvaluatorItem item)
		{
			EvaluatorItem[] items = { item };
			ConstructEvaluator(items);
		}

		public string GetTypeName(Type type)
		{
			try
			{
				//Inner classes?
				var typeName = type == null 
					//|| type.FullName == null
					? null
					: type.FullName.Replace('+', '.').SplitOnFirst('`')[0];

				if (typeName == null) return null;

				if (type.IsGenericType()
					//FIX: support GenericTypeDefinition properly
					&& !type.IsGenericTypeDefinition 
				)
				{
					var genericArgs = type.GetGenericArguments();

					typeName += "<";
					var i = 0;
					foreach (var genericArg in genericArgs)
					{
						if (i++ > 0)
							typeName += ", ";
						typeName += GetTypeName(genericArg);
					}
					typeName += ">";
				}

				return typeName;
			}
			catch (Exception ex)
			{
				//Console.WriteLine(ex);
				throw;
			}
		}

		private void ConstructEvaluator(IEnumerable<EvaluatorItem> items)
		{
			//var codeCompiler = new CSharpCodeProvider(new Dictionary<string, string> { { "CompilerVersion", "v3.5" } });
			var codeCompiler = CodeDomProvider.CreateProvider("CSharp");

			var assemblies = new List<Assembly> {
				typeof(string).Assembly,       //"system.dll",
				typeof(XmlDocument).Assembly,  //"system.xml.dll",
				typeof(Expression).Assembly,   //"system.core.dll",
				typeof(AppHostBase).Assembly,  //"ServiceStack.dll",
				typeof(JsConfig).Assembly,     //"ServiceStack.Text.dll",
				typeof(IService<>).Assembly,   //"ServiceStack.Interfaces.dll",
				typeof(Common.UrnId).Assembly,        //"ServiceStack.Common.dll"
			};
			var cp = new CompilerParameters  //(new[] { "mscorlib.dll", "system.core.dll" })
			{
				GenerateExecutable = false,
				GenerateInMemory = true,
			};
			assemblies.ForEach(x => cp.ReferencedAssemblies.Add(x.Location));


			var code = new StringBuilder();
			code.Append(
@"using System;
using System.Text;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using ServiceStack.Markdown;
using ServiceStack.Markdown.Html;

namespace CSharpEval 
{
  public class _Expr
");

			if (this.BaseType != null)
			{
				code.Append("   : " + GetTypeName(this.BaseType));

				if (GenericArgs.Length > 0)
				{
					code.Append("<");
					var i = 0;
					foreach (var genericArg in GenericArgs)
					{
						if (i++ > 0) code.Append(", ");

						code.Append(GetTypeName(genericArg));
					}
					code.AppendLine(">");
				}
			}

			code.AppendLine("  {");

			AddPropertiesToTypeIfAny(code);

			foreach (var item in items)
			{
				var sbParams = new StringBuilder();
				foreach (var param in item.Params)
				{
					if (sbParams.Length > 0)
						sbParams.Append(", ");

					var typeName = GetTypeName(param.Value);
					sbParams.AppendFormat("{0} {1}", typeName, param.Key);

					var typeAssembly = param.Value.Assembly;
					if (!assemblies.Contains(typeAssembly))
					{
						assemblies.Add(typeAssembly);
						cp.ReferencedAssemblies.Add(typeAssembly.Location);
					}
				}

				code.AppendFormat("    public {0} {1}({2})",
					GetTypeName(item.ReturnType), item.Name, sbParams);

				code.AppendLine("    {");
				code.AppendFormat("      return ({0}); \n", item.Expression);
				code.AppendLine("    }");
			}

			code.AppendLine("  }");
			code.AppendLine("}");

			var src = code.ToString();
			var compilerResults = codeCompiler.CompileAssemblyFromSource(cp, src);
			if (compilerResults.Errors.HasErrors)
			{
				var error = new StringBuilder();
				error.Append("Error Compiling Expression: ");
				foreach (CompilerError err in compilerResults.Errors)
				{
					error.AppendFormat("{0}\n", err.ErrorText);
				}
				throw new Exception("Error Compiling Expression: " + error);
			}

			compiledAssembly = compilerResults.CompiledAssembly;
			compiled = compiledAssembly.CreateInstance("CSharpEval._Expr");
			compiledType = compiled.GetType();
			compiledTypeCtorFn = Text.ReflectionExtensions.GetConstructorMethodToCache(compiledType);
		}

		private void AddPropertiesToTypeIfAny(StringBuilder code)
		{
			if (this.TypeProperties != null)
			{
				foreach (var typeProperty in TypeProperties)
				{
					var name = typeProperty.Key;
					var type = typeProperty.Value;
					var typeName = GetTypeName(type);

					var mi = type.GetMember("Instance", BindingFlags.Static | BindingFlags.Public);
					var hasSingleton = mi.Length > 0;

					var returnExpr = hasSingleton
									 ? typeName + ".Instance"
									 : "new " + typeName + "()";

					code.AppendFormat(
					"    public {0} {1} = {2};\n",
					typeName, name, returnExpr);
				}
			}
		}

		public T GetInstance<T>()
		{
			return (T)compiled;
		}

		public object CreateInstance()
		{
			return compiledTypeCtorFn();
		}

		public MethodInfo GetCompiledMethodInfo(string name)
		{
			return compiledType.GetMethod(name);
		}

		public object Evaluate(string name, params object[] exprParams)
		{
			return Evaluate(compiled, name, exprParams);
		}

		public object Evaluate(object instance, string name, params object[] exprParams)
		{
			try
			{
				var mi = compiledType.GetMethod(name);
				return mi.Invoke(instance, exprParams);
			}
			catch (TargetInvocationException ex)
			{
				Console.WriteLine(ex.InnerException);
				throw ex.InnerException;
			}
		}

		public T Eval<T>(string name, params object[] exprParams)
		{
			return (T)Evaluate(name, exprParams);
		}

		public static object Eval(string code)
		{
			var eval = new Evaluator(typeof(object), code, StaticMethodName);
			return eval.Evaluate(StaticMethodName);
		}

		public static T Eval<T>(string code)
		{
			var eval = new Evaluator(typeof(T), code, StaticMethodName);
			return (T)eval.Evaluate(StaticMethodName);
		}
	}

	public class EvaluatorItem
	{
		public EvaluatorItem() { }

		public EvaluatorItem(Type returnType, string name, string expression, IDictionary<string, Type> exprParams)
		{
			ReturnType = returnType;
			Name = name;
			Expression = expression;
			Params = exprParams;
		}

		public Type ReturnType { get; set; }
		public string Name { get; set; }
		public string Expression { get; set; }
		public IDictionary<string, Type> Params { get; set; }
	}
}
