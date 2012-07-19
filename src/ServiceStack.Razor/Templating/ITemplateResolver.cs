namespace ServiceStack.Razor.Templating
{
    /// <summary>
    /// Defines the required contract for implementing a template resolver.
    /// </summary>
    public interface ITemplateResolver
    {
        #region Methods
        /// <summary>
        /// Gets the template with the specified name.
        /// </summary>
        /// <param name="name">The name of the template.</param>
        /// <returns>The string template.</returns>
        string GetTemplate(string name);
        #endregion
    }
}
