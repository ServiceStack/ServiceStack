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
        /// Determine whether a template at the specified path or page name exists.
        /// </summary>
        /// <param name="pagePathOrName"></param>
        /// <returns></returns>
        bool HasTemplate(string pagePathOrName);

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

        /// <summary>
        /// Wait until all pages have been compiled
        /// </summary>
        void EnsureAllCompiled();

        /// <summary>
        /// Reload if watching modified pages and view page is modified
        /// </summary>
        /// <param name="razorPage"></param>
        void ReloadIfNeeeded(ViewPageRef razorPage);
    }
}
