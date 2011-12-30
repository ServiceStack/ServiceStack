using System.Web.Razor.Parser;

namespace ServiceStack.RazorEngine.Compilation
{
	/// <summary>
    /// Defines the required contract for implementing a compiler service factory.
    /// </summary>
    public interface ICompilerServiceFactory
    {
        #region Methods
        /// <summary>
        /// Creates an instance of a compiler service.
        /// </summary>
        /// <param name="language">The language to support in templates.</param>
        /// <param name="strictMode">Strict mode forces parsing exceptions to be thrown.</param>
        /// <param name="markupParser">The markup parser to use.</param>
        /// <returns>An instance of <see cref="ICompilerService"/>.</returns>
        ICompilerService CreateCompilerService(Language language = Language.CSharp, bool strictMode = false, MarkupParser markupParser = null);
        #endregion
    }
}
