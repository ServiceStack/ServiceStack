using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CSharp;
using System.Text;

namespace ServiceStack.WebHost.EndPoints.Support.Markdown
{
	/// <summary>
	/// Expression Evaluator based from:
	/// http://www.codeproject.com/KB/cs/runtime_eval.aspx
	/// </summary>
	public class Evaluator
	{
		const string staticMethodName = "__tmp";
		Type compiledType = null;
		object compiled = null;

		public Evaluator(IEnumerable<EvaluatorItem> items)
		{
			ConstructEvaluator(items);
		}

		public Evaluator(Type returnType, string expression, string name)
			: this(returnType, expression, name, null) {}

		public Evaluator(Type returnType, string expression, string name, SortedDictionary<string,Type> exprParams)
		{
			EvaluatorItem[] items = 
			{
				new EvaluatorItem {
	                ReturnType  = returnType, 
					Expression = expression, 
					Name = name,
					Params = exprParams ?? new SortedDictionary<string, Type>(),
				}
			};
			ConstructEvaluator(items);
		}

		public Evaluator(EvaluatorItem item)
		{
			EvaluatorItem[] items = { item };
			ConstructEvaluator(items);
		}

		private void ConstructEvaluator(IEnumerable<EvaluatorItem> items)
		{
			ICodeCompiler codeCompiler = (new CSharpCodeProvider().CreateCompiler());
			var cp = new CompilerParameters();
			cp.ReferencedAssemblies.Add("system.dll");
			cp.ReferencedAssemblies.Add("system.xml.dll");
			cp.GenerateExecutable = false;
			cp.GenerateInMemory = true;

			var code = new StringBuilder();
			code.AppendLine("using System;");
			code.AppendLine("using System.Text;");
			code.AppendLine("using System.Xml;");
			code.AppendLine("using System.Collections.Generic;");
			code.AppendLine("namespace CSharpEval { ");
			code.AppendLine("  public class _Expr { ");

			foreach (var item in items)
			{
				var sbParams = new StringBuilder();
				foreach (var param in item.Params)
				{
					if (sbParams.Length > 0)
						sbParams.Append(", ");

					sbParams.AppendFormat("{0} {1}", param.Value.FullName, param.Key);
				}

				code.AppendFormat("    public {0} {1}({2})",
					item.ReturnType.Name,
					item.Name,
					sbParams);

				code.AppendLine("    {");
				code.AppendFormat("      return ({0}); ", item.Expression);
				code.AppendLine("    }");
			}

			code.AppendLine("  }");
			code.AppendLine("}");

			var compilerResults = codeCompiler.CompileAssemblyFromSource(cp, code.ToString());
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
		public Type ReturnType { get; set; }
		public string Name { get; set; }
		public string Expression { get; set; }
		public SortedDictionary<string, Type> Params { get; set; }
	}
}
