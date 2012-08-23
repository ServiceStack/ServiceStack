namespace ServiceStack.Razor.Templating
{
    /// <summary>
    /// Defines the required contract for implementing a template.
    /// </summary>
    public interface ITemplate
    {
        void SetModel(object model);

        /// <summary>
        /// Gets the last result of the template.
        /// </summary>
        string Result { get; }

        /// <summary>
        /// Gets or sets the template service.
        /// </summary>
        TemplateService Service { get; set; }

        /// <summary>
        /// Clears the last result of the template.
        /// </summary>
        void Clear();

        /// <summary>
        /// Executes the compiled template.
        /// </summary>
        void Execute();

        /// <summary>
        /// Writes the specified object to the template result.
        /// </summary>
        /// <param name="object">The object to write.</param>
        void Write(object @object);

        /// <summary>
        /// Writes the specified string to the template result.
        /// </summary>
        /// <param name="string">The string to write.</param>
        void WriteLiteral(string @string);
    }
}