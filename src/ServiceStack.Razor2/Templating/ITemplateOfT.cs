namespace ServiceStack.Razor2.Templating
{
    /// <summary>
    /// Defines the required contract for implementing a template with a model.
    /// </summary>
    /// <typeparam name="TModel">The model type.</typeparam>
    public interface ITemplate<TModel> : ITemplate
    {
        /// <summary>
        /// Gets or sets the template model.
        /// </summary>
        TModel Model { get; set; }
    }
}