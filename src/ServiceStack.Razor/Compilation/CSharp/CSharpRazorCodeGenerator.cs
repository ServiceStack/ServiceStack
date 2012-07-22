using ServiceStack.Razor.Templating;
using System.Web.Razor;
using System.Web.Razor.Parser.SyntaxTree;

namespace ServiceStack.Razor.Compilation.CSharp
{
	/// <summary>
    /// Defines a code generator that supports C# syntax.
    /// </summary>
    public partial class CSharpRazorCodeGenerator : System.Web.Razor.Generator.CSharpRazorCodeGenerator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CSharpRazorCodeGenerator"/> class.
        /// </summary>
        /// <param name="className">Name of the class.</param>
        /// <param name="rootNamespaceName">Name of the root namespace.</param>
        /// <param name="sourceFileName">Name of the source file.</param>
        /// <param name="host">The host.</param>
        /// <param name="strictMode">Flag to specify that this generator is running in struct mode.</param>
        public CSharpRazorCodeGenerator(string className, string rootNamespaceName, string sourceFileName, RazorEngineHost host, bool strictMode)
            : base(className, rootNamespaceName, sourceFileName, host)
        {
            StrictMode = strictMode;
        }

        /// <summary>
        /// Gets whether the code generator is running in strict mode.
        /// </summary>
        public bool StrictMode { get; private set; }

        /// <summary>
        /// Visits an error generated through parsing.
        /// </summary>
        /// <param name="err">The error that was generated.</param>
        public override void VisitError(RazorError err)
        {
            if (StrictMode)
                throw new TemplateParsingException(err);
        }
    }

}