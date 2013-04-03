using System;
using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
using System.Linq;

namespace ServiceStack.Razor.Templating
{
	/// <summary>
    /// Defines an exception that occurs during compilation of a template.
    /// </summary>
    public class TemplateCompilationException : Exception
    {
        /// <summary>
        /// Initialises a new instance of <see cref="TemplateCompilationException"/>
        /// </summary>
        /// <param name="errors">The collection of compilation errors.</param>
        public TemplateCompilationException(CompilerErrorCollection errors)
            : base("Unable to compile template. Check the Errors list for details.")
        {
            var list = errors.Cast<CompilerError>().ToList();
            Errors = new ReadOnlyCollection<CompilerError>(list);
        }

        /// <summary>
        /// Gets the collection of compiler errors.
        /// </summary>
        public ReadOnlyCollection<CompilerError> Errors { get; private set; }
    }
}