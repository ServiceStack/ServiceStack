using System;

namespace ServiceStack.RazorEngine.Templating
{
    /// <summary>
    /// Defines a template resolver that uses a delegate to resolve a named template.
    /// </summary>
    internal class DelegateTemplateResolver : ITemplateResolver
    {
        #region Fields
        private readonly Func<string, string> Resolver;
        #endregion

        #region Constructor
        /// <summary>
        /// Initialises a new instance of <see cref="DelegateTemplateResolver"/>.
        /// </summary>
        /// <param name="resolver">The resolver delegate.</param>
        public DelegateTemplateResolver(Func<string, string> resolver)
        {
            if (resolver == null)
                throw new ArgumentNullException("resolver");

            Resolver = resolver;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Gets the template with the specified name.
        /// </summary>
        /// <param name="name">The name of the template.</param>
        /// <returns>The string template.</returns>
        public string GetTemplate(string name)
        {
            return Resolver(name);
        }
        #endregion
    }
}