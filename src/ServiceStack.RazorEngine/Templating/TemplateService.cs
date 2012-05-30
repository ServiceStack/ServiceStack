using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ServiceStack.RazorEngine.Compilation;

namespace ServiceStack.RazorEngine.Templating
{
    /// <summary>
    /// Defines a templating service.
    /// </summary>
    public partial class TemplateService
    {
        #region Fields
        private IActivator activator;
        private readonly ICompilerService compilerService;
        private readonly IDictionary<string, ITemplate> templateCache = new ConcurrentDictionary<string, ITemplate>();
        private readonly IList<ITemplateResolver> templateResolvers = new List<ITemplateResolver>();
        private Type templateType;
        private readonly object syncObject = new object();
        private readonly object syncObject2 = new object();
        #endregion

        #region Constructor
        /// <summary>
        /// Initialises a new instance of <see cref="ICompilerService"/>.
        /// </summary>
        /// <param name="compilerService">The compiler service used to generate types.</param>
        /// <param name="templateType">The template base type.</param>
        public TemplateService(ICompilerService compilerService, Type templateType = null)
        {
            if (compilerService == null)
                throw new ArgumentNullException("compilerService");

            this.activator = new DefaultActivator();
            this.compilerService = compilerService;
            this.templateType = templateType;

            Namespaces = new HashSet<string> { "System", "System.Collections.Generic", "System.Linq" };
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the collection of namespaces.
        /// </summary>
        public ISet<string> Namespaces { get; private set; }
        #endregion

        #region Methods
        /// <summary>
        /// Adds a resolver used to resolve named template content.
        /// </summary>
        /// <param name="resolver">The resolver to add.</param>
        public void AddResolver(ITemplateResolver resolver)
        {
            if (resolver == null)
                throw new ArgumentNullException("resolver");

            templateResolvers.Add(resolver);
        }

        /// <summary>
        /// Adds a resolver used to resolve named template content.
        /// </summary>
        /// <param name="resolverDelegate">The resolver delegate to add.</param>
        public void AddResolver(Func<string, string> resolverDelegate)
        {
            if (resolverDelegate == null)
                throw new ArgumentNullException("resolverDelegate");

            templateResolvers.Add(new DelegateTemplateResolver(resolverDelegate));
        }

        /// <summary>
        /// Pre-compiles the specified template and caches it using the specified name.
        /// </summary>
        /// <param name="template">The template to precompile.</param>
        /// <param name="name">The cache name for the template.</param>
        public void Compile(string template, string name)
        {
            Compile(template, null, name);
        }

        /// <summary>
        /// Pre-compiles the specified template and caches it using the specified name.
        /// </summary>
        /// <param name="template">The template to precompile.</param>
        /// <param name="modelType">The type of model used in the template.</param>
        /// <param name="name">The cache name for the template.</param>
        public void Compile(string template, Type modelType, string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Pre-compiled templates must have a name", "name");

            var instance = CreateTemplate(template, modelType);

            if (templateCache.ContainsKey(name))
                templateCache[name] = instance;
            else
                templateCache.Add(name, instance);
        }

        /// <summary>
        /// Pre-compiles the specified template and caches it using the specified name.
        /// This method should be used when an anonymous model is used in the template.
        /// </summary>
        /// <param name="template">The template to precompile.</param>
        /// <param name="name">The cache name for the template.</param>
        public void CompileWithAnonymous(string template, string name)
        {
            Compile(template, new { }.GetType(), name);
        }

        /// <summary>
        /// Creates an instance of <see cref="ITemplate"/> from the specified template.
        /// </summary>
        /// <param name="template">The template to compile.</param>
        /// <param name="modelType">The model type.</param>
        /// <returns>An instance of <see cref="ITemplate"/>.</returns>
        internal ITemplate CreateTemplate(string template, Type modelType)
        {
            var context = new TypeContext
                              {
                                  TemplateType = templateType,
                                  TemplateContent = template,
                                  ModelType = modelType
                              };

            foreach (string @namespace in Namespaces)
                context.Namespaces.Add(@namespace);

            Type instanceType = compilerService.CompileType(context);
            var instance = activator.CreateInstance(instanceType);

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

            if (templateResolvers.Count > 0)
            {
                string template = null;
                foreach (var resolver in templateResolvers)
                {
                    template = resolver.GetTemplate(name);
                    if (template != null)
                        break;
                }

                if (template == null)
                    throw new InvalidOperationException("Unable to resolve template with name '" + name + "'");

                return Parse(template, name);
            }

            throw new InvalidOperationException("Unable to resolve template with name '" + name + "'");
        }

        /// <summary>
        /// Resolves the specified template.
        /// </summary>
        /// <typeparam name="T">The model type.</typeparam>
        /// <param name="name">The name of the template.</param>
        /// <param name="model">The model to merged into the template.</param>
        /// <returns>The parsed template content of the named template to include.</returns>
        internal string ResolveTemplate<T>(string name, T model)
        {
            if (templateCache.ContainsKey(name))
                return Run(model, name);

            if (templateResolvers.Count > 0)
            {
                string template = null;
                foreach (var resolver in templateResolvers)
                {
                    template = resolver.GetTemplate(name);
                    if (template != null)
                        break;
                }

                if (template == null)
                    throw new InvalidOperationException("Unable to resolve template with name '" + name + "'");

                return Parse(template, model, name);
            }

            throw new InvalidOperationException("Unable to resolve template with name '" + name + "'");
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
                    return templateCache[name];

            var instance = CreateTemplate(template, modelType);

            if (!string.IsNullOrEmpty(name))
                if (!templateCache.ContainsKey(name))
                    templateCache.Add(name, instance);

            return instance;
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

            SetService(instance, this);
            instance.Execute();

            return instance.Result;
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

            SetService(instance, this);
            SetModel(instance, model);
            instance.Execute();

            return instance.Result;
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

            ITemplate instance;
            if (!templateCache.TryGetValue(name, out instance))
                throw new ArgumentException("No compiled template exists with the specified name.");

            SetService(instance, this);
            instance.Execute();

            return instance.Result;
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

            ITemplate instance;
            if (!templateCache.TryGetValue(name, out instance))
                throw new ArgumentException("No compiled template exists with the specified name.");

            SetService(instance, this);
            SetModel(instance, model);
            instance.Execute();

            return instance.Result;
        }

        /// <summary>
        /// Sets the activator used to create types.
        /// </summary>
        /// <param name="activator">The activator to use.</param>
        public void SetActivator(IActivator activator)
        {
            if (activator == null)
                throw new ArgumentNullException("activator");

            SetActivatorInternal(activator);
        }

        /// <summary>
        /// Sets the activator delegate used to create types.
        /// </summary>
        /// <param name="activatorDelegate">The activator delegate to use.</param>
        public void SetActivator(Func<Type, ITemplate> activatorDelegate)
        {
            if (activator == null)
                throw new ArgumentNullException("activatorDelegate");

            SetActivatorInternal(new DelegateActivator(activatorDelegate));
        }
        
        /// <summary>
        /// Sets the activator used to create types.
        /// </summary>
        /// <param name="activator">The activator to use.</param>
        private void SetActivatorInternal(IActivator activator)
        {
            lock (syncObject2)
            {
                this.activator = activator;
            }
        }

        /// <summary>
        /// Sets the model in the template.
        /// </summary>
        /// <typeparam name="T">The model type.</typeparam>
        /// <param name="template">The template instance to set the model on.</param>
        /// <param name="model">The model.</param>
        private static void SetModel<T>(ITemplate template, T model)
        {
            var dynamicModel = template as ITemplate<dynamic>;
            if (dynamicModel != null)
                dynamicModel.Model = model;

            var strictModel = template as ITemplate<T>;
            if (strictModel != null)
                strictModel.Model = model;
        }

        /// <summary>
        /// Sets the service in the template.
        /// </summary>
        /// <param name="template">The template to set the service on.</param>
        /// <param name="service">The template service managing the template.</param>
        private static void SetService(ITemplate template, TemplateService service)
        {
            template.Service = service;
        }

        /// <summary>
        /// Sets the template base type.
        /// </summary>
        /// <param name="type">The template base type.</param>
        public void SetTemplateBase(Type type)
        {
            if (type == null)
                throw new ArgumentException("type");

            lock (syncObject)
            {
                templateType = type;
            }
        }
        #endregion
    }
}
