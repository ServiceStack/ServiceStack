using System.Web.Razor;
using System.Web.Razor.Generator;

namespace ServiceStack.Razor.Compilation.CSharp
{
	public class CSharpRazorCodeLanguage : System.Web.Razor.CSharpRazorCodeLanguage
    {
        /// <summary>
        /// Initialises a new instance
        /// </summary>
        /// <param name="strictMode">Flag to determine whether strict mode is enabled.</param>
        public CSharpRazorCodeLanguage(bool strictMode)
        {
            StrictMode = strictMode;
        }

        /// <summary>
        /// Gets whether strict mode is enabled.
        /// </summary>
        public bool StrictMode { get; private set; }

        /// <summary>
        /// Creates the code generator.
        /// </summary>
        /// <param name="className">Name of the class.</param>
        /// <param name="rootNamespaceName">Name of the root namespace.</param>
        /// <param name="sourceFileName">Name of the source file.</param>
        /// <param name="host">The host.</param>
        /// <returns>An instance of <see cref="RazorCodeGenerator"/>.</returns>
        public override RazorCodeGenerator CreateCodeGenerator(string className, string rootNamespaceName, string sourceFileName, RazorEngineHost host)
        {
            return new CSharpRazorCodeGenerator(className, rootNamespaceName, sourceFileName, host, StrictMode);
        }
    }
}