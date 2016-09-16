using System;
//using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Text;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack.Support.Markdown
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
			return new Evaluator(Items, BaseType, GenericArgs, TypeProperties);
		}
	}

	public class Evaluator
	{
	    private static ILog Log = LogManager.GetLogger(typeof (Evaluator));

		const string StaticMethodName = "__tmp";
		Assembly compiledAssembly;
		Type compiledType = null;
		object compiled = null;
		EmptyCtorDelegate compiledTypeCtorFn;

		private Type BaseType { get; set; }
		private Type[] GenericArgs { get; set; }
		private IDictionary<string, Type> TypeProperties { get; set; }

        public static readonly List<Assembly> Assemblies = new List<Assembly> {
            typeof(string).GetAssembly(),       //"system.dll",
//			typeof(XmlDocument).GetAssembly(),  //"system.xml.dll",
            typeof(System.Web.HtmlString).GetAssembly(), //"system.web.dll",
            typeof(Expression).GetAssembly(),   //"system.core.dll",
            typeof(AppHostBase).GetAssembly(),  //"ServiceStack.dll",
            typeof(JsConfig).GetAssembly(),     //"ServiceStack.Text.dll",
            typeof(IService).GetAssembly(),   //"ServiceStack.Interfaces.dll",
            typeof(UrnId).GetAssembly(), //"ServiceStack.Common.dll"
        };

	    public static readonly List<string> AssemblyNames = new List<string> {
	        "System",
            "System.Text",
//            "System.Xml",
            "System.Web",
            "System.Collections",
            "System.Collections.Generic",
            "System.Linq",
            "System.Linq.Expressions",
            "ServiceStack.Html",
            "ServiceStack.Markdown"                                                                     
        };

        public static readonly Dictionary<string,string> NamespaceAssemblies = new Dictionary<string, string>();

        //Use NamespaceAssemblies if assemblyName is not also its namespace
        public static void AddAssembly(string assemblyName)
        {
            if (NamespaceAssemblies.ContainsKey(assemblyName))
                assemblyName = NamespaceAssemblies[assemblyName];

            if (AssemblyNames.Contains(assemblyName)) return;
            AssemblyNames.Add(assemblyName);

            try {
                var assembly = Assembly.Load(new AssemblyName(assemblyName));
                if (!Assemblies.Contains(assembly))
                    Assemblies.Add(assembly);
            } catch (System.IO.FileNotFoundException) {
                //Possibly the assembly name differs from the namespace name
                try
                {
                    FindNamespaceInLoadedAssemblies(assemblyName);
                }
                catch (Exception ex) {
                    Log.Error("Can't load assembly: " + assemblyName, ex);
                }
            } catch (Exception ex) {
                Log.Error("Can't load assembly: " + assemblyName, ex);
            }
        }

        private static void FindNamespaceInLoadedAssemblies(string assemblyNamespace)
        {
#if !NETSTANDARD1_6
            var assemblies = from assembly in AppDomain.CurrentDomain.GetAssemblies()
                             from type in assembly.GetTypes()
                             where type.Namespace == assemblyNamespace
                             select type.Assembly;

            foreach (var a in assemblies) {
                if (!Assemblies.Contains(a))
                    Assemblies.Add(a);
            }
#endif
        }

        public static Type FindType(string typeName)
        {
            if (typeName == null || typeName.Contains(".")) return null;
            var type = Type.GetType(typeName);
            if (type != null) return type;
            
            foreach (var assembly in Assemblies)
            {
                var searchType = assembly.GetName().Name + "." + typeName;
                type = assembly.GetType(searchType);
                if (type != null) 
                    return type;
            }

            return null;
        }

		public Evaluator(IEnumerable<EvaluatorItem> items)
			: this(items, null, null, null)
		{
		}

		public Evaluator(IEnumerable<EvaluatorItem> items,
			Type baseType, Type[] genericArgs, IDictionary<string, Type> typeProperties)
		{
			this.BaseType = baseType;
			this.GenericArgs = genericArgs ?? TypeConstants.EmptyTypeArray;
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
					: StringBuilderCacheAlt.Allocate()
                        .Append(type.FullName.Replace('+', '.').LeftPart('`'));

				if (typeName == null) return null;

				if (type.HasGenericType()
					//TODO: support GenericTypeDefinition properly
					&& !type.IsGenericTypeDefinition()
				)
				{
					var genericArgs = type.GetGenericArguments();

					typeName.Append("<");
					var i = 0;
					foreach (var genericArg in genericArgs)
					{
						if (i++ > 0)
							typeName.Append(", ");
						typeName.Append(GetTypeName(genericArg));
					}
					typeName.Append(">");
				}

				return StringBuilderCacheAlt.ReturnAndFree(typeName);
			}
			catch (Exception)
			{
				//Console.WriteLine(ex);
				throw;
			}
		}

		private static readonly bool IsVersion4AndUp = Type.GetType("System.Collections.Concurrent.Partitioner") != null;
        
#if NETSTANDARD1_6
		private void ConstructEvaluator(IEnumerable<EvaluatorItem> items) {}
#else
		private static void AddAssembly(System.CodeDom.Compiler.CompilerParameters cp, string location)
		{
			//Error if trying to re-add ref to mscorlib or System.Core for .NET 4.0
			if (IsVersion4AndUp && 
				(location == typeof(string).Assembly.Location
				|| location == typeof(Expression).Assembly.Location))
				return;

			cp.ReferencedAssemblies.Add(location);
		}

        private void ConstructEvaluator(IEnumerable<EvaluatorItem> items)
		{
            var codeCompiler = System.CodeDom.Compiler.CodeDomProvider.CreateProvider("CSharp");

			var cp = new System.CodeDom.Compiler.CompilerParameters 
			{
				GenerateExecutable = false,
				GenerateInMemory = true,
			};
			Assemblies.ForEach(x => AddAssembly(cp, x.Location));
            
			var code = StringBuilderCache.Allocate();

            AssemblyNames.ForEach(x => 
                code.AppendFormat("using {0};\n", x));

			code.Append(
@"

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
						ReferenceTypesIfNotExist(cp, Assemblies, genericArg);
					}
					code.AppendLine(">");
				}
				
				ReferenceTypesIfNotExist(cp, Assemblies, this.BaseType);
			}

			code.AppendLine("  {");

			AddPropertiesToTypeIfAny(code);

			foreach (var item in items)
			{
				var sbParams = StringBuilderCacheAlt.Allocate();
				foreach (var param in item.Params)
				{
					if (sbParams.Length > 0)
						sbParams.Append(", ");

					var typeName = GetTypeName(param.Value);
					sbParams.AppendFormat("{0} {1}", typeName, param.Key);

					var paramType = param.Value;

					ReferenceAssembliesIfNotExists(cp, paramType, Assemblies);
				}

				var isVoid = item.ReturnType == typeof(void);

				var returnType = isVoid ? "void" : GetTypeName(item.ReturnType);
				code.AppendFormat("    public {0} {1}({2})",
					returnType, item.Name, StringBuilderCacheAlt.ReturnAndFree(sbParams));

				code.AppendLine("    {");
				if (isVoid)
				{
					code.AppendFormat("      {0}; \n", item.Expression);
				}
				else
				{
					code.AppendFormat("      return ({0}); \n", item.Expression);
				}
				code.AppendLine("    }");
			}

			code.AppendLine("  }");
			code.AppendLine("}");

			if (IsVersion4AndUp)
			{
				if (!Env.IsMono)
				{
                    cp.ReferencedAssemblies.Add(Env.ReferenceAssembyPath + @"System.Core.dll");
				}
			}

			var src = StringBuilderCache.ReturnAndFree(code);
			var compilerResults = codeCompiler.CompileAssemblyFromSource(cp, src);
			if (compilerResults.Errors.HasErrors)
			{
				var error = StringBuilderCache.Allocate();
				error.Append("Error Compiling Expression: ");
				foreach (System.CodeDom.Compiler.CompilerError err in compilerResults.Errors)
				{
					error.AppendFormat("{0}\n", err.ErrorText);
				}
				throw new Exception("Error Compiling Expression: " + StringBuilderCache.ReturnAndFree(error));
			}

			compiledAssembly = compilerResults.CompiledAssembly;
			compiled = compiledAssembly.CreateInstance("CSharpEval._Expr");
			compiledType = compiled.GetType();
			compiledTypeCtorFn = ReflectionExtensions.GetConstructorMethodToCache(compiledType);
		}

		private static void ReferenceTypesIfNotExist(System.CodeDom.Compiler.CompilerParameters cp, List<Assembly> assemblies, params Type[] paramTypes)
		{
			foreach (var paramType in paramTypes)
			{
				if (!assemblies.Contains(paramType.Assembly))
				{
					assemblies.Add(paramType.Assembly);
					AddAssembly(cp, paramType.Assembly.Location);
				}
			}
		}

		private static void ReferenceAssembliesIfNotExists(System.CodeDom.Compiler.CompilerParameters cp, Type paramType, List<Assembly> assemblies)
		{
			var typeAssemblies = new List<Assembly>();

			var typeAssembly = paramType.Assembly;
			if (!assemblies.Contains(typeAssembly))
				typeAssemblies.Add(typeAssembly);

			if (paramType.IsGenericType)
			{
				var genericArgs = paramType.GetGenericArguments();
				typeAssemblies.AddRange(genericArgs.Select(x => x.Assembly));
			}

			foreach (var assembly in typeAssemblies)
			{
				assemblies.Add(assembly);

                if (assembly.IsDynamic()) continue;
				AddAssembly(cp, assembly.Location);
			}
		}
#endif

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

					code.AppendFormat("    public {0} {1} = {2};\n", typeName, name, returnExpr);
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
