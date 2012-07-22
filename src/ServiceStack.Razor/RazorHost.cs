using System;
using ServiceStack.Razor.Templating;

namespace ServiceStack.Razor
{
    /// <summary>
    /// Provides quick access to template services.
    /// </summary>
    public static class RazorHost
    {
        /// <summary>
        /// Gets the default template service.
        /// </summary>
        public static TemplateService TemplateService { get; private set; }

        /// <summary>
        /// Pre-compiles the specified template and caches it using the specified name.
        /// </summary>
        /// <param name="template">The template to precompile.</param>
        /// <param name="name">The cache name for the template.</param>
        public static void Compile(string template, string name)
        {
            TemplateService.CompileWithAnonymous(template, name);
        }

        /// <summary>
        /// Pre-compiles the specified template and caches it using the specified name.
        /// </summary>
        /// <param name="template">The template to precompile.</param>
        /// <param name="modelType">The type of model used in the template.</param>
        /// <param name="name">The cache name for the template.</param>
        public static void Compile(string template, Type modelType, string name)
        {
            TemplateService.Compile(template, modelType, name);
        }

        /// <summary>
        /// Pre-compiles the specified template and caches it using the specified name.
        /// This method should be used when an anonymous model is used in the template.
        /// </summary>
        /// <param name="template">The template to precompile.</param>
        /// <param name="name">The cache name for the template.</param>
        public static void CompileWithAnonymous(string template, string name)
        {
            TemplateService.CompileWithAnonymous(template, name);
        }

        /// <summary>
        /// Configures the templating engine.
        /// </summary>
        public static void Configure(TemplateService service)
        {
            TemplateService = service;
        }

        /// <summary>
        /// Parses the given template and returns the result.
        /// </summary>
        /// <param name="template">The template to parse.</param>
        /// <param name="name">[Optional] The name of the template. This is used to cache the template.</param>
        /// <returns>The string result of the parsed template.</returns>
        public static string Parse(string template, string name = null)
        {
            return TemplateService.Parse(template, name);
        }

        /// <summary>
        /// Parses the given template and returns the result.
        /// </summary>
        /// <typeparam name="T">The model type.</typeparam>
        /// <param name="template">The template to parse.</param>
        /// <param name="model">The model.</param>
        /// <param name="name">[Optional] The name of the template. This is used to cache the template.</param>
        /// <returns>The string result of the parsed template.</returns>
        public static string Parse<T>(string template, T model, string name = null)
        {
            return TemplateService.Parse<T>(template, model, name);
        }

        /// <summary>
        /// Runs the template with the specified name.
        /// </summary>
        /// <param name="name">The name of the template to run.</param>
        /// <returns>The result of the template.</returns>
        public static string Run(string name)
        {
            return TemplateService.Run(name);
        }

        /// <summary>
        /// Runs the template with the specified name.
        /// </summary>
        /// <typeparam name="T">The model type.</typeparam>
        /// <param name="model">The model.</param>
        /// <param name="name">The name of the template to run.</param>
        /// <returns>The result of the template.</returns>
        public static string Run<T>(T model, string name)
        {
            return TemplateService.Run<T>(model, name);
        }
        
        /// <summary>
        /// Sets the template base type.
        /// </summary>
        /// <param name="type">The template base type.</param>
        public static void SetTemplateBase(Type type)
        {
            TemplateService.TemplateBaseType = type;
        }
    }
}