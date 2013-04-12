using System.CodeDom.Compiler;
using System.Web.Razor.Parser.SyntaxTree;
using ServiceStack.Common.Extensions;
using ServiceStack.Razor2.Templating;
using ServiceStack.Text;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Razor;
using System.Web.Razor.Generator;

namespace ServiceStack.Razor2.Compilation
{
    /// <summary>
    /// Provides a base implementation of a compiler service.
    /// </summary>
    public abstract class CompilerServiceBase
    {
        private readonly CodeDomProvider CodeDomProvider;

        protected CompilerServiceBase(
            CodeDomProvider codeDomProvider,
            RazorCodeLanguage codeLanguage)
        {
            if (codeLanguage == null)
                throw new ArgumentNullException("codeLanguage");
            
            CodeDomProvider = codeDomProvider;
            CodeLanguage = codeLanguage;
        }

        /// <summary>
        /// Gets the code language.
        /// </summary>
        public RazorCodeLanguage CodeLanguage { get; private set; }


        /// <summary>
        /// Builds a type name for the specified template type and model type.
        /// </summary>
        /// <param name="templateType">The template type.</param>
        /// <param name="modelType">The model type.</param>
        /// <returns>The string type name (including namespace).</returns>
        public virtual string BuildTypeName(Type templateType, Type modelType)
        {
            if (templateType == null)
                throw new ArgumentNullException("templateType");

            if (!templateType.IsGenericTypeDefinition && !templateType.IsGenericType)
                return templateType.FullName;

            if (modelType == null)
                throw new ArgumentException("The template type is a generic defintion, and no model type has been supplied.");

            bool @dynamic = CompilerServices.IsDynamicType(modelType);
            Type genericType = templateType.MakeGenericType(modelType);

            return BuildTypeNameInternal(genericType, @dynamic);
        }

        /// <summary>
        /// Builds a type name for the specified generic type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="isDynamic">Is the model type dynamic?</param>
        /// <returns>The string typename (including namespace and generic type parameters).</returns>
        public abstract string BuildTypeNameInternal(Type type, bool isDynamic);

		static string[] DuplicatedAssmebliesInMono = new string[] {
			"mscorlib.dll",
			"System/4.0.0.0__b77a5c561934e089/System.dll",
			"System.Xml/4.0.0.0__b77a5c561934e089/System.Xml.dll",
			"System.Core/4.0.0.0__b77a5c561934e089/System.Core.dll",
			"Microsoft.CSharp/4.0.0.0__b03f5f7f11d50a3a/Microsoft.CSharp.dll",
		};

        /// <summary>
        /// Creates the compile results for the specified <see cref="TypeContext"/>.
        /// </summary>
        /// <param name="context">The type context.</param>
        /// <returns>The compiler results.</returns>
        private CompilerResults Compile(TypeContext context)
        {
            var compileUnit = GetCodeCompileUnit(
                context.ClassName,
                context.TemplateContent,
                context.Namespaces,
                context.TemplateType,
                context.ModelType);

            var @params = new CompilerParameters {
                GenerateInMemory = true,
                GenerateExecutable = false,
                IncludeDebugInformation = false,
                CompilerOptions = "/target:library /optimize",
                TempFiles = { KeepFiles = true }
            };

            var assemblies = CompilerServices
                .GetLoadedAssemblies()
                .Where(a => !a.IsDynamic)
                .Select(a => a.Location)
                .ToArray();

            @params.ReferencedAssemblies.AddRange(assemblies);

			if (Env.IsMono)
			{
				for (var i=@params.ReferencedAssemblies.Count-1; i>=0; i--)
				{
					var assembly = @params.ReferencedAssemblies[i];
					foreach (var filterAssembly in DuplicatedAssmebliesInMono)
					{
						if (assembly.Contains(filterAssembly)) {
							@params.ReferencedAssemblies.RemoveAt(i);
						}
					}
				}
			}

            var results = CodeDomProvider.CompileAssemblyFromDom( @params, compileUnit );

            //Tricky: Don't forget to cleanup. 
            // Simply setting KeepFiles = false and then calling
            // dispose on the parent TempFilesCollection won't
            // clean up. So, create a new collection and
            // explicitly mark the files for deletion.
            var tempFilesMarkedForDeletion = new TempFileCollection(null);
            @params.TempFiles
                   .OfType<string>()
                   .ForEach( file => tempFilesMarkedForDeletion.AddFile( file, false ) );

            using ( tempFilesMarkedForDeletion )
            {
                if ( results.Errors != null && results.Errors.HasErrors )
                {
                    throw new TemplateCompilationException( results );
                }

                return results;
            }
        }

        public Type CompileType(TypeContext context)
        {
            var results = Compile(context);

            return results.CompiledAssembly.GetType("CompiledRazorTemplates.Dynamic." + context.ClassName);
        }

        /// <summary>
        /// Generates any required constructors for the specified type.
        /// </summary>
        /// <param name="constructors">The set of constructors.</param>
        /// <param name="codeType">The code type declaration.</param>
        private static void GenerateConstructors(IEnumerable<ConstructorInfo> constructors, CodeTypeDeclaration codeType)
        {
            if (constructors == null || !constructors.Any())
                return;

            var existingConstructors = codeType.Members.OfType<CodeConstructor>().ToArray();
            foreach (var existingConstructor in existingConstructors)
                codeType.Members.Remove(existingConstructor);

            foreach (var constructor in constructors)
            {
                var ctor = new CodeConstructor { Attributes = MemberAttributes.Public };

                foreach (var param in constructor.GetParameters())
                {
                    ctor.Parameters.Add(new CodeParameterDeclarationExpression(param.ParameterType, param.Name));
                    ctor.BaseConstructorArgs.Add(new CodeSnippetExpression(param.Name));
                }

                codeType.Members.Add(ctor);
            }
        }

        /// <summary>
        /// Gets the code compile unit used to compile a type.
        /// </summary>
        /// <param name="className">The class name.</param>
        /// <param name="template">The template to compile.</param>
        /// <param name="namespaceImports">The set of namespace imports.</param>
        /// <param name="templateType">The template type.</param>
        /// <param name="modelType">The model type.</param>
        /// <returns>A <see cref="CodeCompileUnit"/> used to compile a type.</returns>
        public CodeCompileUnit GetCodeCompileUnit(string className, string template, ISet<string> namespaceImports, Type templateType, Type modelType)
        {
            if (string.IsNullOrEmpty(className))
                throw new ArgumentException("Class name is required.");

            if (string.IsNullOrEmpty(template))
                throw new ArgumentException("Template is required.");

            templateType = templateType
                ?? ((modelType == null)
                        ? typeof(TemplateBase)
                        : typeof(TemplateBase<>));

            var host = new RazorEngineHost(CodeLanguage)
            {
                DefaultBaseClass = BuildTypeName(templateType, modelType),
                DefaultClassName = className,
                DefaultNamespace = "CompiledRazorTemplates.Dynamic",
                GeneratedClassContext = new GeneratedClassContext(
                    "Execute", "Write", "WriteLiteral",
                    "WriteTo", "WriteLiteralTo",
                    "ServiceStack.Razor2.Templating.TemplateWriter",
                    "WriteSection")
                    {
                        ResolveUrlMethodName = "Href"
                    }
            };

            var templateNamespaces = templateType.GetCustomAttributes(typeof(RequireNamespacesAttribute), true)
                .Cast<RequireNamespacesAttribute>()
                .SelectMany(att => att.Namespaces);

            foreach (string ns in templateNamespaces)
                namespaceImports.Add(ns);

            foreach (string @namespace in namespaceImports)
                host.NamespaceImports.Add(@namespace);

            var engine = new RazorTemplateEngine(host);
            GeneratorResults result;
            using (var reader = new StringReader(template))
            {
                result = engine.GenerateCode(reader);
            }

            var type = result.GeneratedCode.Namespaces[0].Types[0];
            if (modelType != null)
            {
                if (CompilerServices.IsAnonymousType(modelType))
                {
                    type.CustomAttributes.Add(new CodeAttributeDeclaration(
                        new CodeTypeReference(typeof(HasDynamicModelAttribute))));
                }
            }

            GenerateConstructors(CompilerServices.GetConstructors(templateType), type);

            var statement = new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "Clear");
            foreach (CodeTypeMember member in type.Members)
            {
                if (member.Name.Equals("Execute"))
                {
                    ((CodeMemberMethod)member).Statements.Insert(0, new CodeExpressionStatement(statement));
                    break;
                }
            }

            return result.GeneratedCode;
        }

        public IEnumerable<T> AllNodesOfType<T>(Block block)
        {
            if (block is T)
                yield return (T)(object)block;

            foreach (var syntaxTreeNode in block.Children)
            {
                if (syntaxTreeNode is T)
                    yield return (T)(object)syntaxTreeNode;

                var childBlock = syntaxTreeNode as Block;
                if (childBlock == null) continue;

                foreach (var variable in AllNodesOfType<T>(childBlock))
                {
                    yield return variable;
                }
            }
        }
    }
}