using System;
using ServiceStack.Html;

namespace ServiceStack.Razor.Templating
{
    /// <summary>
    /// Defines the required contract for implementing a template resolver.
    /// </summary>
    public interface IRazorViewEngine : IViewEngine
    {
        /// <summary>
        /// Creates an instance of the specified type.
        /// </summary>
        /// <param name="type">The type to create an instance.</param>
        ITemplate CreateInstance(Type type);
        
        /// <summary>
        /// Gets the template with the specified name.
        /// </summary>
        /// <param name="name">The name of the template.</param>
        /// <returns>The string template.</returns>
        string GetTemplate(string name);

        /// <summary>
        /// Gets the view page with the specified name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        IViewPage GetView(string name);
    }
}
