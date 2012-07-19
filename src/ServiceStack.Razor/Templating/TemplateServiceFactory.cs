using System;
using System.Configuration;
using System.Web.Razor.Parser;
using ServiceStack.Razor.Configuration;

namespace ServiceStack.Razor.Templating
{
	/// <summary>
    /// Provides factory methods for creating <see cref="TemplateService"/> instances.
    /// </summary>
    public static class TemplateServiceFactory
    {
        #region Methods
        /// <summary>
        /// Creates an instance of a template service.
        /// </summary>
        /// <param name="language">The language the template service to support.</param>
        /// <param name="strictMode">Specifies whether parsing exceptions will be thrown.</param>
        /// <param name="markupParser">The markup parser.</param>
        /// <returns>An instance of <see cref="TemplateService"/>.</returns>
        public static TemplateService CreateTemplateService(Language language = Language.CSharp, bool strictMode = false, MarkupParser markupParser = null)
        {
            var factory = RazorHost.CompilerServiceFactory;
            var compilerService = factory.CreateCompilerService(language, strictMode, markupParser);

            return new TemplateService(compilerService);
        }

        /// <summary>
        /// Creates an instance of a template service from configuration.
        /// </summary>
        /// <param name="config">The configuration of the template service.</param>
        /// <returns>An instance of <see cref="TemplateService"/>.</returns>
        public static TemplateService CreateTemplateService(TemplateServiceConfigurationElement config)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            MarkupParser parser = null;
            if (!string.IsNullOrWhiteSpace(config.MarkupParser))
                parser = CreateMarkupParser(config.MarkupParser);

            return CreateTemplateService(config.Language, config.StrictMode, parser);
        }

        /// <summary>
        /// Create an instance of <see cref="MarkupParser"/>.
        /// </summary>
        /// <param name="typeName">The <see cref="MarkupParser"/> type.</param>
        /// <returns>An instance of <see cref="MarkupParser"/>.</returns>
        private static MarkupParser CreateMarkupParser(string typeName)
        {
            Type parserType = Type.GetType(typeName);
            if (parserType == null)
                throw new ConfigurationErrorsException("The parser type '" + typeName + "' could not be loaded.");

            MarkupParser parser = Activator.CreateInstance(parserType) as MarkupParser;
            if (parser == null)
                throw new ConfigurationErrorsException("The type '" + typeName + "' is not a markup parser.");

            return parser;
        }
        #endregion
    }
}
