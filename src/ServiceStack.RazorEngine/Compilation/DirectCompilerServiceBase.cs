using ServiceStack.RazorEngine.Templating;
using ServiceStack.Text;
using System;
using System.CodeDom.Compiler;
using System.Linq;
using System.Web.Razor;
using System.Web.Razor.Parser;

namespace ServiceStack.RazorEngine.Compilation
{
	/// <summary>
    /// Provides a base implementation of a direct compiler service.
    /// </summary>
    public abstract class DirectCompilerServiceBase : CompilerServiceBase
    {
        #region Fields
        private readonly CodeDomProvider CodeDomProvider;
        #endregion

        #region Constructor
        /// <summary>
        /// Initialises a new instance of <see cref="DirectCompilerServiceBase"/>.
        /// </summary>
        /// <param name="codeLanguage">The razor code language.</param>
        /// <param name="codeDomProvider">The code dom provider used to generate code.</param>
        /// <param name="markupParser">The markup parser.</param>
        protected DirectCompilerServiceBase(RazorCodeLanguage codeLanguage, CodeDomProvider codeDomProvider, MarkupParser markupParser)
            : base(codeLanguage, markupParser)
        {

            if (codeDomProvider == null)
                throw new ArgumentNullException("codeDomProvider");

            CodeDomProvider = codeDomProvider;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Creates the compile results for the specified <see cref="TypeContext"/>.
        /// </summary>
        /// <param name="context">The type context.</param>
        /// <returns>The compiler results.</returns>
        private CompilerResults Compile(TypeContext context)
        {
            var compileUnit = GetCodeCompileUnit(context.ClassName, context.TemplateContent, context.Namespaces,
                                                 context.TemplateType, context.ModelType);

            var @params = new CompilerParameters
                              {
                                  GenerateInMemory = true,
                                  GenerateExecutable = false,
                                  IncludeDebugInformation = false,
                                  CompilerOptions = "/target:library /optimize"
                              };

            var assemblies = CompilerServices
                .GetLoadedAssemblies()
                .Where(a => !a.IsDynamic)
                .Select(a => a.Location)
                .ToArray();

            @params.ReferencedAssemblies.AddRange(assemblies);

            return CodeDomProvider.CompileAssemblyFromDom(@params, compileUnit);
        }

        /// <summary>
        /// Compiles the type defined in the specified type context.
        /// </summary>
        /// <param name="context">The type context which defines the type to compile.</param>
        /// <returns>The compiled type.</returns>
        public override Type CompileType(TypeContext context)
        {
            var results = Compile(context);

            if (results.Errors != null && results.Errors.Count > 0)
            {
				Console.WriteLine(results.Errors.Dump());
				throw new TemplateCompilationException(results.Errors);
			}

            return results.CompiledAssembly.GetType("CompiledRazorTemplates.Dynamic." + context.ClassName);
        }
        #endregion
    }
}