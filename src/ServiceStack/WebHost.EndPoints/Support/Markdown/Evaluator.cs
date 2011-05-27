using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using Microsoft.CSharp;
using System.Text;
using ServiceStack.Common.Utils;
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
		}

		public List<EvaluatorItem> Items { get; private set; }

		public Evaluator Build(Type baseType, IDictionary<string, Type> typeProperties)
		{
			return Items.Count == 0
				? null
				: new Evaluator(Items, baseType, typeProperties);
		}
	}

	/// <summary>
	/// Expression Evaluator based from:
	/// http://www.codeproject.com/KB/cs/runtime_eval.aspx
	/// </summary>
	public class Evaluator
	{
		const string staticMethodName = "__tmp";
		Type compiledType = null;
		object compiled = null;

		private Type BaseType { get; set; }
		private IDictionary<string, Type> TypeProperties { get; set; }

		public Evaluator(IEnumerable<EvaluatorItem> items)
			: this(items, null, null)
		{
		}

		public Evaluator(IEnumerable<EvaluatorItem> items,
			Type baseType, IDictionary<string, Type> typeProperties)
		{
			this.BaseType = baseType;
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
			//Inner classes?
			return type == null ? null : type.FullName.Replace('+', '.');
		}

		private void ConstructEvaluator(IEnumerable<EvaluatorItem> items)
		{
			var codeCompiler = CodeDomProvider.CreateProvider("CSharp");
			var cp = new CompilerParameters();

			var assemblies = new List<Assembly> {
				typeof(string).Assembly,       //"system.dll",
				typeof(XmlDocument).Assembly,  //"system.xml.dll",
				typeof(AppHostBase).Assembly,  //"ServiceStack.dll",
				typeof(JsConfig).Assembly,     //"ServiceStack.Text.dll",
				typeof(IService<>).Assembly,   //"ServiceStack.Interfaces.dll",
				typeof(IdUtils).Assembly,      //"ServiceStack.Common.dll"
			};
			assemblies.ForEach(x => cp.ReferencedAssemblies.Add(x.Location));

			cp.GenerateExecutable = false;
			cp.GenerateInMemory = true;

			var code = new StringBuilder();
			code.Append(
@"using System;
using System.Text;
using System.Xml;
using System.Collections;
using System.Collections.Generic;

namespace CSharpEval 
{
  public class _Expr
");

			if (this.BaseType != null)
				code.AppendLine("   : " + GetTypeName(this.BaseType));

			code.AppendLine("  {");

			AddPropertiesToTypeIfAny(code);

			foreach (var item in items)
			{
				var sbParams = new StringBuilder();
				foreach (var param in item.Params)
				{
					if (sbParams.Length > 0)
						sbParams.Append(", ");

					sbParams.AppendFormat("{0} {1}", GetTypeName(param.Value), param.Key);

					var typeAssembly = param.Value.Assembly;
					if (!assemblies.Contains(typeAssembly))
					{
						assemblies.Add(typeAssembly);
						cp.ReferencedAssemblies.Add(typeAssembly.Location);
					}
				}

				code.AppendFormat("    public {0} {1}({2})",
					item.ReturnType.Name, item.Name, sbParams);

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

			var compiledAssembly = compilerResults.CompiledAssembly;
			compiled = compiledAssembly.CreateInstance("CSharpEval._Expr");
			compiledType = compiled.GetType();
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
					"    public {0} {1} {{ get {{ return {2}; }} }}\n",
					typeName, name, returnExpr);
				}
			}
		}

		public T GetInstance<T>()
		{
			return (T)compiled;
		}

		public MethodInfo GetCompiledMethodInfo(string name)
		{
			return compiledType.GetMethod(name);
		}

		public object Evaluate(string name, params object[] exprParams)
		{
			var mi = compiledType.GetMethod(name);
			return mi.Invoke(compiled, exprParams);
		}

		public T Eval<T>(string name, params object[] exprParams)
		{
			return (T)Evaluate(name, exprParams);
		}

		public static object Eval(string code)
		{
			var eval = new Evaluator(typeof(object), code, staticMethodName);
			return eval.Evaluate(staticMethodName);
		}

		public static T Eval<T>(string code)
		{
			var eval = new Evaluator(typeof(T), code, staticMethodName);
			return (T)eval.Evaluate(staticMethodName);
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
