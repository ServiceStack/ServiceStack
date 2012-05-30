using System;
using System.Web.Razor.Parser;
using ServiceStack.RazorEngine.Compilation.CSharp;

namespace ServiceStack.RazorEngine.Compilation
{
    /// <summary>
    /// Provides a default implementation of a compiler service factory.
    /// </summary>
    public class DefaultCompilerServiceFactory : ICompilerServiceFactory
    {
        #region Methods
        /// <summary>
        /// Creates an instance of a compiler service.
        /// </summary>
        /// <param name="language">The language to support in templates.</param>
        /// <param name="strictMode">Strict mode forces parsing exceptions to be thrown.</param>
        /// <param name="markupParser">The markup parser to use.</param>
        /// <returns>An instance of <see cref="ICompilerService"/>.</returns>
        public ICompilerService CreateCompilerService(Language language = Language.CSharp, bool strictMode = false, MarkupParser markupParser = null)
        {
            switch (language)
            {
                case Language.CSharp:
                    return new CSharpDirectCompilerService(strictMode, markupParser);
            }

            throw new ArgumentException("The language '" + language + "' is not supported.");
        }
        #endregion
    }
}