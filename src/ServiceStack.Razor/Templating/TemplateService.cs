using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ServiceStack.Razor.Compilation;
using ServiceStack.Razor.Compilation.CSharp;

namespace ServiceStack.Razor.Templating
{
	/// <summary>
    /// Defines a templating service.
    /// </summary>
    public partial class TemplateService
	{
	    private readonly IRazorViewEngine viewEngine;

        public Type TemplateBaseType { get; set; }

        public Func<CompilerServiceBase> GetCompilerServiceFn;

        private readonly IDictionary<string, ITemplate> templateCache = new ConcurrentDictionary<string, ITemplate>();
        private readonly IDictionary<string, ViewPageRef> templateRefCache = new ConcurrentDictionary<string, ViewPageRef>();

        public TemplateService(IRazorViewEngine viewEngine, Type templateBaseType = null)
        {
            this.GetCompilerServiceFn = GetCompilerService;
            this.viewEngine = viewEngine;
            this.TemplateBaseType = templateBaseType ?? typeof(ViewPage<>);
            this.Namespaces = new HashSet<string>();
        }

        public CompilerServiceBase GetCompilerService()
        {
            return new CSharpDirectCompilerService();
        }

        /// <summary>
        /// Gets the collection of namespaces.
        /// </summary>
        public HashSet<string> Namespaces { get; set; }

        /// <summary>
        /// Entry point for tests
        /// </summary>
        /// <param name="template"></param>
        /// <param name="name"></param>
        public void Compile(string template, string name)
        {
            Compile(null, template, null, name);
        }

	    /// <summary>
	    /// Pre-compiles the specified template and caches it using the specified name.
	    /// </summary>
	    /// <param name="viewPageRef"> </param>
	    /// <param name="template">The template to precompile.</param>
	    /// <param name="name">The cache name for the template.</param>
	    public void Compile(ViewPageRef viewPageRef, string template, string name)
        {
            Compile(viewPageRef, template, null, name);
        }

	    /// <summary>
	    /// Pre-compiles the specified template and caches it using the specified name.
	    /// </summary>
	    /// <param name="viewPageRef"> </param>
	    /// <param name="template">The template to precompile.</param>
	    /// <param name="modelType">The type of model used in the template.</param>
	    /// <param name="name">The cache name for the template.</param>
	    public void Compile(ViewPageRef viewPageRef, string template, Type modelType, string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Pre-compiled templates must have a name", "name");

			if (modelType == null)
				modelType = new { }.GetType();

            var instance = CreateTemplate(template, modelType);
			templateCache[name] = instance;
            if (viewPageRef != null) templateRefCache[name] = viewPageRef;
        }

	    /// <summary>
	    /// Pre-compiles the specified template and caches it using the specified name.
	    /// This method should be used when an anonymous model is used in the template.
	    /// </summary>
	    /// <param name="viewPageRef"> </param>
	    /// <param name="template">The template to precompile.</param>
	    /// <param name="name">The cache name for the template.</param>
	    public void CompileWithAnonymous(ViewPageRef viewPageRef, string template, string name)
        {
            Compile(viewPageRef, template, new { }.GetType(), name);
        }

        /// <summary>
        /// Creates an instance of <see cref="ITemplate"/> from the specified template.
        /// </summary>
        /// <param name="template">The template to compile.</param>
        /// <param name="modelType">The model type.</param>
        /// <returns>An instance of <see cref="ITemplate"/>.</returns>
        internal ITemplate CreateTemplate(string template, Type modelType)
        {
            var context = new TypeContext {
                TemplateType = TemplateBaseType,
                TemplateContent = template,
                ModelType = modelType
            };

            foreach (string @namespace in Namespaces)
                context.Namespaces.Add(@namespace);

            var compilerService = GetCompilerServiceFn();
            Type instanceType = compilerService.CompileType(context);
            var instance = viewEngine.CreateInstance(instanceType);

            return instance;
        }

        /// <summary>
        /// Resolves the specified template.
        /// </summary>
        /// <param name="name">The name of the template to resolve.</param>
        /// <returns>The parsed template content of the named template to include.</returns>
        internal string ResolveTemplate(string name)
        {
            if (templateCache.ContainsKey(name))
                return Run(name);

            var template = viewEngine.GetTemplate(name);
            
            if (template == null)
                throw new InvalidOperationException("Unable to resolve template with name '" + name + "'");

            return Parse(template, name);
        }

        internal string ResolveTemplate<T>(string name, T model)
        {
            if (templateCache.ContainsKey(name))
                return Run(model, name);

            var template = viewEngine.GetTemplate(name);

            if (template == null)
                throw new InvalidOperationException("Unable to resolve template with name '" + name + "'");

            return Parse(template, model, name);
        }

        /// <summary>
        /// Gets an <see cref="ITemplate"/> for the specified template.
        /// </summary>
        /// <param name="template">The template to parse.</param>
        /// <param name="modelType">The model to use in the template.</param>
        /// <param name="name">The name of the template.</param>
        /// <returns>An instance of <see cref="ITemplate"/>.</returns>
        internal ITemplate GetTemplate(string template, Type modelType, string name)
        {
            if (!string.IsNullOrEmpty(name))
                if (templateCache.ContainsKey(name))
                    return templateCache[name].CloneTemplate();

            var instance = CreateTemplate(template, modelType);

            if (!string.IsNullOrEmpty(name))
                if (!templateCache.ContainsKey(name))
                    templateCache.Add(name, instance);

            return instance.CloneTemplate();
        }

        /// <summary>
        /// Parses the given template and returns the result.
        /// </summary>
        /// <param name="template">The template to parse.</param>
        /// <param name="name">[Optional] The name of the template. This is used to cache the template.</param>
        /// <returns>The string result of the parsed template.</returns>
        public string Parse(string template, string name = null)
        {
            var instance = GetTemplate(template, null, name);
            using (instance as IDisposable)
            {
                SetService(instance, this);
                instance.Execute();

                return instance.Result;
            }
        }

        /// <summary>
        /// Parses the given template and returns the result.
        /// </summary>
        /// <typeparam name="T">The model type.</typeparam>
        /// <param name="template">The template to parse.</param>
        /// <param name="model">The model.</param>
        /// <param name="name">[Optional] The name of the template. This is used to cache the template.</param>
        /// <returns>The string result of the parsed template.</returns>
        public string Parse<T>(string template, T model, string name = null)
        {
            var instance = GetTemplate(template, typeof(T), name);
            using (instance as IDisposable)
            {
                SetService(instance, this);
                SetModel(instance, model);
                instance.Execute();

                return instance.Result;
            }
        }

        /// <summary>
        /// Runs the template with the specified name.
        /// </summary>
        /// <param name="name">The name of the template to run.</param>
        /// <returns>The result of the template.</returns>
        public string Run(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("The named of the cached template is required.");

            var instance = GetAndCheckTemplate(name);
            if (instance == null)
                throw new ArgumentException("No compiled template exists with the specified name.");

            using (instance as IDisposable)
            {
                SetService(instance, this);
                instance.Execute();

                return instance.Result;
            }
        }

        /// <summary>
        /// Runs the template with the specified name.
        /// </summary>
        /// <typeparam name="T">The model type.</typeparam>
        /// <param name="model">The model.</param>
        /// <param name="name">The name of the template to run.</param>
        /// <returns>The result of the template.</returns>
        public string Run<T>(T model, string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("The named of the cached template is required.");

            var instance = GetAndCheckTemplate(name);
            if (instance == null)
                throw new ArgumentException("No compiled template exists with the specified name.");

            using (instance as IDisposable)
            {
                SetService(instance, this);
                SetModel(instance, model);
                instance.Execute();

                return instance.Result;
            }
        }

        /// <summary>
        /// Sets the model in the template.
        /// </summary>
        /// <typeparam name="T">The model type.</typeparam>
        /// <param name="template">The template instance to set the model on.</param>
        /// <param name="model">The model.</param>
        internal static void SetModel<T>(ITemplate template, T model)
        {
            var dynamicModel = template as ITemplate<dynamic>;
            if (dynamicModel != null)
                dynamicModel.Model = model;

            var strictModel = template as ITemplate<T>;
            if (strictModel != null)
                strictModel.Model = model;

            if (strictModel == null && dynamicModel == null)
            {
                template.SetModel(model);
            }
        }

        /// <summary>
        /// Sets the service in the template.
        /// </summary>
        /// <param name="template">The template to set the service on.</param>
        /// <param name="service">The template service managing the template.</param>
        internal static void SetService(ITemplate template, TemplateService service)
        {
            template.Service = service;
        }
    }

}
