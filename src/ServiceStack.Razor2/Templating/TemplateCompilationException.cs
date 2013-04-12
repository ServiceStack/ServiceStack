using System;
using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Web;

namespace ServiceStack.Razor2.Templating
{
	/// <summary>
    /// Defines an exception that occurs during compilation of a template.
    /// </summary>
    public class TemplateCompilationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of <see cref="TemplateCompilationException"/>
        /// </summary>
        /// <param name="results">The compiler results.</param>
        public TemplateCompilationException(CompilerResults results)
            : base("Unable to compile template. Check the Errors list for details.")
        {
            var list = results.Errors.Cast<CompilerError>().ToList();
            Errors = new ReadOnlyCollection<CompilerError>(list);

            //check if source file exists, read it.
            //HttpCompileException is sealed by MS. So, we'll
            //just add a property instead of inheriting from it.
            var sourceFile = list.First( ce => !ce.IsWarning ).FileName;

            if ( File.Exists( sourceFile ) )
            {
                var sourceCode = File.ReadAllText( sourceFile );
                this.HttpCompileException = new HttpCompileException( results, sourceCode );
            }
        }

        /// <summary>
        /// Gets the collection of compiler errors.
        /// </summary>
        public ReadOnlyCollection<CompilerError> Errors { get; private set; }

        /// <summary>
        /// The HttpCompileException is not null when razor source code is available.
        /// </summary>
        public HttpCompileException HttpCompileException { get; private set; }
    }
}